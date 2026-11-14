using FixFlow.Data;
using FixFlow.Models;
using FixFlow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Pages.Jobs;

[Authorize(Policy = "AllStaff")]
public class PrintModel(FixFlowDbContext database, JobAccessService jobAccessService) : PageModel
{
    public RepairJob Job { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var job = await database.RepairJobs.Include(item => item.Customer).Include(item => item.Equipment).Include(item => item.Technician).Include(item => item.Estimate).SingleOrDefaultAsync(item => item.Id == id);
        if (job is null) return NotFound();
        if (!await jobAccessService.CanAccessJobAsync(User, job)) return Forbid();
        Job = job;
        return Page();
    }
}
