namespace FixFlow.Models;

public enum RepairJobStatus
{
    Received,
    Diagnosing,
    AwaitingApproval,
    InProgress,
    ReadyForPickup,
    Collected,
    Cancelled
}
