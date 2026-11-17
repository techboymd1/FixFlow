using System.Data;
using System.Data.Common;
using FixFlow.Data;
using FixFlow.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FixFlow.Services;

public class PaymentService(FixFlowDbContext database)
{
    private const string ReceiptLockName = "FixFlow:PaymentReceiptSequence";

    public async Task<PaymentResult> RecordPaymentAsync(
        int repairJobId,
        decimal amount,
        string paymentMethod,
        string? notes,
        int receivedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        if (amount <= 0)
        {
            return PaymentResult.Failed("The payment amount must be greater than zero.");
        }

        await using var transaction = await database.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken
        );

        var receiptLockAcquired = false;
        var transactionCommitted = false;

        try
        {
            // This locking read serializes payments for the same repair job.
            var job = await database
                .RepairJobs.FromSqlInterpolated(
                    $"SELECT * FROM `RepairJobs` WHERE `Id` = {repairJobId} FOR UPDATE"
                )
                .SingleOrDefaultAsync(cancellationToken);

            if (job is null)
            {
                return PaymentResult.Failed("The repair job was not found.");
            }

            // Do not rely on navigation collections already loaded by the page handler.
            // These aggregate queries read current committed values after the job lock.
            var estimate = await database
                .RepairEstimates.AsNoTracking()
                .Where(item => item.RepairJobId == repairJobId)
                .Select(item => new { item.PartsAmount, item.LabourAmount })
                .SingleOrDefaultAsync(cancellationToken);

            var usedPartsTotal = await database
                .JobParts.AsNoTracking()
                .Where(item => item.RepairJobId == repairJobId)
                .Select(item => (decimal?)(item.UnitPrice * item.Quantity))
                .SumAsync(cancellationToken)
                ?? 0m;

            var paidTotal = await database
                .Payments.AsNoTracking()
                .Where(item => item.RepairJobId == repairJobId)
                .Select(item => (decimal?)item.Amount)
                .SumAsync(cancellationToken)
                ?? 0m;

            var quotedParts = estimate?.PartsAmount ?? 0m;
            var labourTotal = estimate?.LabourAmount ?? 0m;
            var jobTotal = Math.Max(quotedParts, usedPartsTotal) + labourTotal;
            var outstandingBalance = jobTotal - paidTotal;

            if (amount > outstandingBalance)
            {
                return PaymentResult.Failed(
                    $"The outstanding balance is {outstandingBalance:N2}; a payment cannot exceed it."
                );
            }

            // A single MySQL session lock serializes receipt numbers across different jobs.
            receiptLockAcquired = await AcquireReceiptNumberLockAsync(
                transaction.GetDbTransaction(),
                cancellationToken
            );

            if (!receiptLockAcquired)
            {
                return PaymentResult.Failed(
                    "Receipt generation is busy. Please try recording the payment again."
                );
            }

            var payment = new Payment
            {
                RepairJobId = repairJobId,
                Amount = amount,
                PaymentMethod = paymentMethod,
                Notes = notes,
                ReceiptNumber = await CreateReceiptNumberAsync(cancellationToken),
                ReceivedByUserId = receivedByUserId,
            };

            database.Payments.Add(payment);
            job.UpdatedAtUtc = DateTime.UtcNow;

            await database.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            transactionCommitted = true;

            return PaymentResult.Succeeded(payment.Id);
        }
        finally
        {
            if (receiptLockAcquired)
            {
                await ReleaseReceiptNumberLockAsync(
                    transactionCommitted ? null : transaction.GetDbTransaction()
                );
            }
        }
    }

    public static decimal GetJobTotal(RepairJob job)
    {
        var quotedParts = job.Estimate?.PartsAmount ?? 0m;
        var labourTotal = job.Estimate?.LabourAmount ?? 0m;
        var usedPartsTotal = job.PartsUsed.Sum(part => part.LineTotal);

        // Keep the quoted parts charge unless actual recorded usage is higher.
        return Math.Max(quotedParts, usedPartsTotal) + labourTotal;
    }

    private async Task<bool> AcquireReceiptNumberLockAsync(
        DbTransaction transaction,
        CancellationToken cancellationToken
    )
    {
        var connection = database.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT GET_LOCK(@lockName, 10);";
        command.Transaction = transaction;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@lockName";
        parameter.DbType = DbType.String;
        parameter.Value = ReceiptLockName;
        command.Parameters.Add(parameter);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null && result != DBNull.Value && Convert.ToInt32(result) == 1;
    }

    private async Task ReleaseReceiptNumberLockAsync(DbTransaction? transaction)
    {
        var connection = database.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT RELEASE_LOCK(@lockName);";
        command.Transaction = transaction;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@lockName";
        parameter.DbType = DbType.String;
        parameter.Value = ReceiptLockName;
        command.Parameters.Add(parameter);

        await command.ExecuteScalarAsync(CancellationToken.None);
    }

    private async Task<string> CreateReceiptNumberAsync(CancellationToken cancellationToken)
    {
        var prefix = $"RC-{DateTime.UtcNow:yyyyMMdd}-";

        var existingReceipts = await database
            .Payments.AsNoTracking()
            .Where(payment => payment.ReceiptNumber.StartsWith(prefix))
            .Select(payment => payment.ReceiptNumber)
            .ToListAsync(cancellationToken);

        var highestNumber = existingReceipts
            .Select(receipt =>
                long.TryParse(receipt[prefix.Length..], out var number) ? number : 0L
            )
            .DefaultIfEmpty(0L)
            .Max();

        return $"{prefix}{highestNumber + 1:D3}";
    }
}

public record PaymentResult(bool IsSuccessful, int? PaymentId, string? ErrorMessage)
{
    public static PaymentResult Succeeded(int paymentId) => new(true, paymentId, null);

    public static PaymentResult Failed(string message) => new(false, null, message);
}
