using System.Data;
using FixFlow.Data;
using FixFlow.Models;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Services;

public class InventoryService(FixFlowDbContext database)
{
    public async Task<InventoryResult> UsePartOnJobAsync(
        int jobId,
        int partId,
        int quantity,
        int userId,
        CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            return InventoryResult.Failed("The quantity must be at least 1.");
        }

        await using var transaction = await database.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var part = await database.Parts.SingleOrDefaultAsync(item => item.Id == partId && item.IsActive, cancellationToken);
        if (part is null)
        {
            return InventoryResult.Failed("That part is no longer available.");
        }

        if (part.StockQuantity < quantity)
        {
            return InventoryResult.Failed($"Only {part.StockQuantity} item(s) of {part.Name} are in stock.");
        }

        var job = await database.RepairJobs.SingleOrDefaultAsync(item => item.Id == jobId, cancellationToken);
        if (job is null)
        {
            return InventoryResult.Failed("The repair job was not found.");
        }

        var usedPart = await database.JobParts.SingleOrDefaultAsync(
            item => item.RepairJobId == jobId && item.PartId == partId,
            cancellationToken);

        if (usedPart is null)
        {
            usedPart = new JobPart
            {
                RepairJobId = jobId,
                PartId = partId,
                Quantity = quantity,
                UnitPrice = part.SellingPrice
            };
            database.JobParts.Add(usedPart);
        }
        else
        {
            usedPart.Quantity += quantity;
        }

        part.StockQuantity -= quantity;
        database.StockMovements.Add(new StockMovement
        {
            PartId = partId,
            QuantityChange = -quantity,
            MovementType = "Job usage",
            Reason = $"Used on {job.JobReference}",
            RepairJobId = jobId
        });
        job.UpdatedAtUtc = DateTime.UtcNow;

        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return InventoryResult.Succeeded();
    }

    public async Task<InventoryResult> AdjustStockAsync(
        int partId,
        int quantityChange,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (quantityChange == 0)
        {
            return InventoryResult.Failed("Enter a non-zero adjustment.");
        }

        await using var transaction = await database.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var part = await database.Parts.SingleOrDefaultAsync(item => item.Id == partId, cancellationToken);
        if (part is null)
        {
            return InventoryResult.Failed("The part was not found.");
        }

        if (part.StockQuantity + quantityChange < 0)
        {
            return InventoryResult.Failed("This adjustment would make stock negative.");
        }

        part.StockQuantity += quantityChange;
        database.StockMovements.Add(new StockMovement
        {
            PartId = partId,
            QuantityChange = quantityChange,
            MovementType = "Manual adjustment",
            Reason = reason
        });
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return InventoryResult.Succeeded();
    }
}

public record InventoryResult(bool IsSuccessful, string? ErrorMessage)
{
    public static InventoryResult Succeeded() => new(true, null);
    public static InventoryResult Failed(string message) => new(false, message);
}
