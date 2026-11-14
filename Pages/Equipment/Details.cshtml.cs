using FixFlow.Data;
using FixFlow.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Pages.Equipment;

[Authorize(Policy = "FrontDeskOrAdmin")]
public class DetailsModel(FixFlowDbContext database) : PageModel
{
    public FixFlow.Models.Equipment Equipment { get; private set; } = null!;
    public List<RepairJob> Jobs { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var equipment = await database.Equipment.Include(item => item.Customer).SingleOrDefaultAsync(item => item.Id == id);
        if (equipment is null) return NotFound();
        Equipment = equipment;
        Jobs = await database.RepairJobs.Where(job => job.EquipmentId == id).OrderByDescending(job => job.ReceiveDate).ToListAsync();
        return Page();
    }
}
