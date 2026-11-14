using System.ComponentModel.DataAnnotations;

namespace FixFlow.Models;

public class RepairJob
{
    public int Id { get; set; }

    [Required, StringLength(30)]
    public string JobReference { get; set; } = string.Empty;

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int EquipmentId { get; set; }
    public Equipment Equipment { get; set; } = null!;

    public int? TechnicianId { get; set; }
    public AppUser? Technician { get; set; }

    [DataType(DataType.Date)]
    public DateTime ReceiveDate { get; set; } = DateTime.Today;

    [Required, StringLength(1500)]
    public string ReportedFault { get; set; } = string.Empty;

    [StringLength(1500)]
    public string? ConditionAndAccessories { get; set; }

    [DataType(DataType.Date)]
    public DateTime? ExpectedCompletionDate { get; set; }

    public RepairJobStatus Status { get; set; } = RepairJobStatus.Received;

    [StringLength(1000)]
    public string? InternalNotes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public RepairEstimate? Estimate { get; set; }
    public ICollection<JobNote> Notes { get; set; } = new List<JobNote>();
    public ICollection<JobPart> PartsUsed { get; set; } = new List<JobPart>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<JobStatusHistory> StatusHistory { get; set; } = new List<JobStatusHistory>();
}
