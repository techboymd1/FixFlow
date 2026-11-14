using FixFlow.Data;
using FixFlow.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Pages.Technicians;

[Authorize(Roles = UserRoles.Technician)]
public class QueueModel(FixFlowDbContext database, FixFlow.Services.JobAccessService jobAccessService) : PageModel
{
    public List<RepairJob> Jobs { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var userId = jobAccessService.GetCurrentUserId(User);
        Jobs = await database.RepairJobs.Include(job => job.Customer).Include(job => job.Equipment)
            .Where(job => job.TechnicianId == userId && job.Status != RepairJobStatus.Collected && job.Status != RepairJobStatus.Cancelled)
            .OrderBy(job => job.ExpectedCompletionDate == null).ThenBy(job => job.ExpectedCompletionDate).ThenBy(job => job.ReceiveDate)
            .ToListAsync();
    }
}
