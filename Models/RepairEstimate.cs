using System.ComponentModel.DataAnnotations;

namespace FixFlow.Models;

public class RepairEstimate
{
    public int Id { get; set; }
    public int RepairJobId { get; set; }
    public RepairJob RepairJob { get; set; } = null!;

    [Range(0, 999999999)]
    public decimal PartsAmount { get; set; }

    [Range(0, 999999999)]
    public decimal LabourAmount { get; set; }

    [StringLength(1200)]
    public string? Description { get; set; }

    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Pending;
    public DateTime? RespondedAtUtc { get; set; }

    [StringLength(500)]
    public string? ResponseNotes { get; set; }

    public decimal TotalAmount => PartsAmount + LabourAmount;
}
