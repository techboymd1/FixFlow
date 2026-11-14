using System.ComponentModel.DataAnnotations;

namespace FixFlow.Models;

public class Payment
{
    public int Id { get; set; }
    public int RepairJobId { get; set; }
    public RepairJob RepairJob { get; set; } = null!;

    [Range(0.01, 999999999)]
    public decimal Amount { get; set; }

    [Required, StringLength(30)]
    public string PaymentMethod { get; set; } = "Cash";

    [StringLength(80)]
    public string ReceiptNumber { get; set; } = string.Empty;

    [StringLength(250)]
    public string? Notes { get; set; }

    public DateTime PaidAtUtc { get; set; } = DateTime.UtcNow;
    public int ReceivedByUserId { get; set; }
    public AppUser ReceivedByUser { get; set; } = null!;
}
