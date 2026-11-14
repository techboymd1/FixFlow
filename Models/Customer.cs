using System.ComponentModel.DataAnnotations;

namespace FixFlow.Models;

public class Customer
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    [Display(Name = "Customer name")]
    public string FullName { get; set; } = string.Empty;

    [StringLength(30), Phone]
    public string? PhoneNumber { get; set; }

    [StringLength(120), EmailAddress]
    public string? Email { get; set; }

    [StringLength(250)]
    public string? Address { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<Equipment> Equipment { get; set; } = new List<Equipment>();
}
