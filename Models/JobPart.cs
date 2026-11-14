using System.ComponentModel.DataAnnotations;

namespace FixFlow.Models;

public class JobPart
{
    public int Id { get; set; }
    public int RepairJobId { get; set; }
    public RepairJob RepairJob { get; set; } = null!;
    public int PartId { get; set; }
    public Part Part { get; set; } = null!;

    [Range(1, 999999)]
    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal LineTotal => UnitPrice * Quantity;
}
