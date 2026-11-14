using System.ComponentModel.DataAnnotations;

namespace FixFlow.Models;

public class Part
{
    public int Id { get; set; }

    [Required, StringLength(50)]
    public string Sku { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Range(0, 999999)]
    public int StockQuantity { get; set; }

    [Range(0, 999999)]
    public int ReorderPoint { get; set; }

    [Range(0, 999999999)]
    public decimal CostPrice { get; set; }

    [Range(0, 999999999)]
    public decimal SellingPrice { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<JobPart> JobParts { get; set; } = new List<JobPart>();
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}
