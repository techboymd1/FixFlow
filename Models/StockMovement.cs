using System.ComponentModel.DataAnnotations;

namespace FixFlow.Models;

public class StockMovement
{
    public int Id { get; set; }

    public int PartId { get; set; }
    public Part Part { get; set; } = null!;

    public int QuantityChange { get; set; }

    [Required, StringLength(30)]
    public string MovementType { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Reason { get; set; }

    public int? RepairJobId { get; set; }
    public RepairJob? RepairJob { get; set; }

    public int? CreatedByUserId { get; set; }
    public AppUser? CreatedByUser { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}