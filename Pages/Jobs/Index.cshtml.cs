using FixFlow.Data;
using FixFlow.Models;
using FixFlow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Pages.Jobs;

[Authorize(Policy = "AllStaff")]
public class IndexModel(FixFlowDbContext database) : PageModel
{
    public string? Search { get; private set; }
    public string? Status { get; private set; }
    public bool OpenOnly { get; private set; }
    public bool UnpaidOnly { get; private set; }
    public List<RepairJob> Jobs { get; private set; } = [];
    public Dictionary<int, decimal> Balances { get; private set; } = [];
    public List<SelectListItem> StatusOptions { get; } = Enum.GetValues<RepairJobStatus>()
        .Select(status => new SelectListItem(JobWorkflowService.FormatStatus(status), status.ToString())).ToList();

    public async Task OnGetAsync(string? search, string? status, bool openOnly, bool unpaidOnly)
    {
        Search = search?.Trim();
        Status = status;
        OpenOnly = openOnly;
        UnpaidOnly = unpaidOnly && !User.IsInRole(UserRoles.Technician);
        var jobs = database.RepairJobs
            .Include(job => job.Customer).Include(job => job.Equipment).Include(job => job.Technician)
            .Include(job => job.Estimate).Include(job => job.PartsUsed).Include(job => job.Payments)
            .AsQueryable();

        if (User.IsInRole(UserRoles.Technician))
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            jobs = jobs.Where(job => job.TechnicianId == userId);
        }
        if (!string.IsNullOrWhiteSpace(Search))
        {
            jobs = jobs.Where(job => job.JobReference.Contains(Search) || job.Customer.FullName.Contains(Search) ||
                job.Equipment.Category.Contains(Search) || (job.Equipment.Brand != null && job.Equipment.Brand.Contains(Search)) ||
                (job.Equipment.Model != null && job.Equipment.Model.Contains(Search)) || job.ReportedFault.Contains(Search));
        }
        if (Enum.TryParse<RepairJobStatus>(status, out var selectedStatus)) jobs = jobs.Where(job => job.Status == selectedStatus);
        if (openOnly) jobs = jobs.Where(job => job.Status != RepairJobStatus.Collected && job.Status != RepairJobStatus.Cancelled);

        Jobs = await jobs.OrderByDescending(job => job.UpdatedAtUtc).ToListAsync();
        Balances = Jobs.ToDictionary(job => job.Id, job => PaymentService.GetJobTotal(job) - job.Payments.Sum(payment => payment.Amount));
        if (UnpaidOnly) Jobs = Jobs.Where(job => Balances[job.Id] > 0).ToList();
        foreach (var option in StatusOptions) option.Selected = option.Value == Status;
    }
}
