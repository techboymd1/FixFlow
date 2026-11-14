using FixFlow.Data;
using FixFlow.Models;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Services;

public class JobWorkflowService(FixFlowDbContext database)
{
    private static readonly IReadOnlyDictionary<RepairJobStatus, RepairJobStatus[]> AllowedTransitions =
        new Dictionary<RepairJobStatus, RepairJobStatus[]>
        {
            [RepairJobStatus.Received] = [RepairJobStatus.Diagnosing, RepairJobStatus.Cancelled],
            [RepairJobStatus.Diagnosing] = [RepairJobStatus.AwaitingApproval, RepairJobStatus.Cancelled],
            [RepairJobStatus.AwaitingApproval] = [RepairJobStatus.InProgress, RepairJobStatus.Cancelled],
            [RepairJobStatus.InProgress] = [RepairJobStatus.ReadyForPickup, RepairJobStatus.Cancelled],
            [RepairJobStatus.ReadyForPickup] = [RepairJobStatus.Collected, RepairJobStatus.Cancelled],
            [RepairJobStatus.Collected] = [],
            [RepairJobStatus.Cancelled] = []
        };

    public IReadOnlyList<RepairJobStatus> GetAllowedNextStatuses(RepairJobStatus currentStatus)
    {
        return AllowedTransitions[currentStatus];
    }

    public async Task<WorkflowResult> TransitionAsync(
        RepairJob job,
        RepairJobStatus nextStatus,
        int userId,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        if (!AllowedTransitions[job.Status].Contains(nextStatus))
        {
            return WorkflowResult.Failed($"{FormatStatus(nextStatus)} is not a valid next status for this job.");
        }

        if (nextStatus == RepairJobStatus.InProgress)
        {
            var isApproved = await database.RepairEstimates
                .AnyAsync(estimate => estimate.RepairJobId == job.Id && estimate.ApprovalStatus == ApprovalStatus.Approved, cancellationToken);

            if (!isApproved)
            {
                return WorkflowResult.Failed("An approved estimate is required before repair work can begin.");
            }
        }

        var previousStatus = job.Status;
        job.Status = nextStatus;
        job.UpdatedAtUtc = DateTime.UtcNow;
        database.JobStatusHistories.Add(new JobStatusHistory
        {
            RepairJobId = job.Id,
            FromStatus = previousStatus,
            ToStatus = nextStatus,
            ChangedByUserId = userId,
            Notes = notes
        });

        await database.SaveChangesAsync(cancellationToken);
        return WorkflowResult.Succeeded();
    }

    public static string FormatStatus(RepairJobStatus status)
    {
        return status switch
        {
            RepairJobStatus.AwaitingApproval => "Awaiting Approval",
            RepairJobStatus.InProgress => "In Progress",
            RepairJobStatus.ReadyForPickup => "Ready for Pickup",
            _ => status.ToString()
        };
    }
}

public record WorkflowResult(bool IsSuccessful, string? ErrorMessage)
{
    public static WorkflowResult Succeeded() => new(true, null);
    public static WorkflowResult Failed(string message) => new(false, message);
}
