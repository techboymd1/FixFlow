using System.Data;
using System.Data.Common;
using FixFlow.Data;
using FixFlow.Models;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Services;

public class JobReferenceService(FixFlowDbContext database)
{
    public async Task CreateJobWithNextReferenceAsync(
        RepairJob job,
        int createdByUserId,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(job);

        var prefix = $"FF-{job.ReceiveDate:yyyyMMdd}-";
        var lockName = $"FixFlow:JobReference:{job.ReceiveDate:yyyyMMdd}";
        var connection = database.Database.GetDbConnection();
        var openedConnectionHere = connection.State != ConnectionState.Open;

        if (openedConnectionHere)
        {
            await database.Database.OpenConnectionAsync(cancellationToken);
        }

        var lockAcquired = false;

        try
        {
            lockAcquired = await AcquireLockAsync(connection, lockName, cancellationToken);
            if (!lockAcquired)
            {
                throw new InvalidOperationException(
                    "Unable to obtain the job reference lock. Please try creating the job again."
                );
            }

            var existingReferences = await database
                .RepairJobs.AsNoTracking()
                .Where(existing => existing.JobReference.StartsWith(prefix))
                .Select(existing => existing.JobReference)
                .ToListAsync(cancellationToken);

            var highestNumber = existingReferences
                .Select(reference =>
                    long.TryParse(reference[prefix.Length..], out var number) ? number : 0L
                )
                .DefaultIfEmpty(0L)
                .Max();

            job.JobReference = $"{prefix}{highestNumber + 1:D3}";
            database.RepairJobs.Add(job);

            // Save the job and its initial history entry in the same EF transaction.
            database.JobStatusHistories.Add(
                new JobStatusHistory
                {
                    RepairJob = job,
                    FromStatus = job.Status,
                    ToStatus = job.Status,
                    ChangedByUserId = createdByUserId,
                    Notes = "Job card created.",
                }
            );

            await database.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            try
            {
                if (lockAcquired)
                {
                    await ReleaseLockAsync(connection, lockName);
                }
            }
            finally
            {
                if (openedConnectionHere)
                {
                    await database.Database.CloseConnectionAsync();
                }
            }
        }
    }

    private static async Task<bool> AcquireLockAsync(
        DbConnection connection,
        string lockName,
        CancellationToken cancellationToken
    )
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT GET_LOCK(@lockName, 30);";
        command.CommandTimeout = 35;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@lockName";
        parameter.DbType = DbType.String;
        parameter.Value = lockName;
        command.Parameters.Add(parameter);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null && result != DBNull.Value && Convert.ToInt32(result) == 1;
    }

    private static async Task ReleaseLockAsync(DbConnection connection, string lockName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT RELEASE_LOCK(@lockName);";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@lockName";
        parameter.DbType = DbType.String;
        parameter.Value = lockName;
        command.Parameters.Add(parameter);

        await command.ExecuteScalarAsync(CancellationToken.None);
    }
}
