using System.ComponentModel.DataAnnotations;

namespace FixFlow.Models;

public class JobStatusHistory
{
    public int Id { get; set; }
    public int RepairJobId { get; set; }
    public RepairJob RepairJob { get; set; } = null!;
    public RepairJobStatus FromStatus { get; set; }
    public RepairJobStatus ToStatus { get; set; }
    public int ChangedByUserId { get; set; }
    public AppUser ChangedByUser { get; set; } = null!;

    [StringLength(500)]
    public string? Notes { get; set; }

    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
}
