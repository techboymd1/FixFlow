using FixFlow.Data;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Services;

public class JobReferenceService(FixFlowDbContext database)
{
    public async Task<string> CreateNextReferenceAsync(DateTime receiveDate, CancellationToken cancellationToken = default)
    {
        var prefix = $"FF-{receiveDate:yyyyMMdd}-";
        var lastReference = await database.RepairJobs
            .Where(job => job.JobReference.StartsWith(prefix))
            .OrderByDescending(job => job.JobReference)
            .Select(job => job.JobReference)
            .FirstOrDefaultAsync(cancellationToken);

        var nextNumber = 1;
        if (lastReference is not null && int.TryParse(lastReference[prefix.Length..], out var lastNumber))
        {
            nextNumber = lastNumber + 1;
        }

        return $"{prefix}{nextNumber:D3}";
    }
}
