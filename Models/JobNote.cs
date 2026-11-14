using System.ComponentModel.DataAnnotations;

namespace FixFlow.Models;

public class JobNote
{
    public int Id { get; set; }
    public int RepairJobId { get; set; }
    public RepairJob RepairJob { get; set; } = null!;

    public int CreatedByUserId { get; set; }
    public AppUser CreatedByUser { get; set; } = null!;

    [Required, StringLength(2000)]
    public string NoteText { get; set; } = string.Empty;

    public bool IsDiagnosis { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
