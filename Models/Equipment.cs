using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace FixFlow.Models;

public class Equipment
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    [ValidateNever]
    public Customer Customer { get; set; } = null!;

    [Required, StringLength(50)]
    public string Category { get; set; } = string.Empty;

    [StringLength(80)]
    public string? Brand { get; set; }

    [StringLength(100)]
    public string? Model { get; set; }

    [StringLength(100)]
    public string? SerialNumber { get; set; }

    [StringLength(80)]
    public string? Capacity { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<RepairJob> RepairJobs { get; set; } = new List<RepairJob>();
}