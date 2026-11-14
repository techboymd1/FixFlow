using FixFlow.Data;
using FixFlow.Models;
using FixFlow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Pages;

[Authorize(Policy = "AllStaff")]
public class IndexModel(FixFlowDbContext database) : PageModel
{
    public int OpenJobCount { get; private set; }
    public int WaitingApprovalCount { get; private set; }
    public int ReadyForPickupCount { get; private set; }
    public int UnpaidJobCount { get; private set; }
    public List<RepairJob> RecentJobs { get; private set; } = [];
    public List<Part> LowStockParts { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var currentUserId = User.IsInRole(UserRoles.Technician)
            ? int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value)
            : (int?)null;

        var visibleJobs = database.RepairJobs
            .Include(job => job.Customer)
            .Include(job => job.Equipment)
            .Include(job => job.Estimate)
            .Include(job => job.PartsUsed)
            .Include(job => job.Payments)
            .AsQueryable();

        if (currentUserId.HasValue)
        {
            visibleJobs = visibleJobs.Where(job => job.TechnicianId == currentUserId);
        }

        var jobs = await visibleJobs.ToListAsync();
        OpenJobCount = jobs.Count(job => job.Status is not RepairJobStatus.Collected and not RepairJobStatus.Cancelled);
        WaitingApprovalCount = jobs.Count(job => job.Status == RepairJobStatus.AwaitingApproval);
        ReadyForPickupCount = jobs.Count(job => job.Status == RepairJobStatus.ReadyForPickup);
        UnpaidJobCount = jobs.Count(job => PaymentService.GetJobTotal(job) > job.Payments.Sum(payment => payment.Amount));
        RecentJobs = jobs.OrderByDescending(job => job.UpdatedAtUtc).Take(8).ToList();

        if (!User.IsInRole(UserRoles.Technician))
        {
            LowStockParts = await database.Parts
                .Where(part => part.IsActive && part.StockQuantity <= part.ReorderPoint)
                .OrderBy(part => part.StockQuantity)
                .Take(8)
                .ToListAsync();
        }
    }
}
