using FixFlow.Data;
using FixFlow.Models;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Services;

public class PaymentService(FixFlowDbContext database)
{
    public async Task<PaymentResult> RecordPaymentAsync(
        int repairJobId,
        decimal amount,
        string paymentMethod,
        string? notes,
        int receivedByUserId,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
        {
            return PaymentResult.Failed("The payment amount must be greater than zero.");
        }

        var job = await database.RepairJobs
            .Include(item => item.Estimate)
            .Include(item => item.PartsUsed)
            .Include(item => item.Payments)
            .SingleOrDefaultAsync(item => item.Id == repairJobId, cancellationToken);

        if (job is null)
        {
            return PaymentResult.Failed("The repair job was not found.");
        }

        var jobTotal = GetJobTotal(job);
        var paidTotal = job.Payments.Sum(payment => payment.Amount);
        var outstandingBalance = jobTotal - paidTotal;
        if (amount > outstandingBalance)
        {
            return PaymentResult.Failed($"The outstanding balance is {outstandingBalance:N2}; a payment cannot exceed it.");
        }

        var payment = new Payment
        {
            RepairJobId = repairJobId,
            Amount = amount,
            PaymentMethod = paymentMethod,
            Notes = notes,
            ReceiptNumber = await CreateReceiptNumberAsync(cancellationToken),
            ReceivedByUserId = receivedByUserId
        };
        database.Payments.Add(payment);
        job.UpdatedAtUtc = DateTime.UtcNow;
        await database.SaveChangesAsync(cancellationToken);
        return PaymentResult.Succeeded(payment.Id);
    }

    public static decimal GetJobTotal(RepairJob job)
    {
        var quotedParts = job.Estimate?.PartsAmount ?? 0m;
        var labourTotal = job.Estimate?.LabourAmount ?? 0m;
        var usedPartsTotal = job.PartsUsed.Sum(part => part.LineTotal);

        // Keep the quoted parts charge unless actual recorded usage is higher.
        // Labour is always retained when parts usage exceeds the original quote.
        return Math.Max(quotedParts, usedPartsTotal) + labourTotal;
    }

    private async Task<string> CreateReceiptNumberAsync(CancellationToken cancellationToken)
    {
        var prefix = $"RC-{DateTime.UtcNow:yyyyMMdd}-";
        var lastReceipt = await database.Payments
            .Where(payment => payment.ReceiptNumber.StartsWith(prefix))
            .OrderByDescending(payment => payment.ReceiptNumber)
            .Select(payment => payment.ReceiptNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var nextNumber = 1;
        if (lastReceipt is not null && int.TryParse(lastReceipt[prefix.Length..], out var lastNumber))
        {
            nextNumber = lastNumber + 1;
        }

        return $"{prefix}{nextNumber:D3}";
    }
}

public record PaymentResult(bool IsSuccessful, int? PaymentId, string? ErrorMessage)
{
    public static PaymentResult Succeeded(int paymentId) => new(true, paymentId, null);
    public static PaymentResult Failed(string message) => new(false, null, message);
}
