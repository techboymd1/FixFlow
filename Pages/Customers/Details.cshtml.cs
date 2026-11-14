using FixFlow.Data;
using FixFlow.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Pages.Customers;

[Authorize(Policy = "FrontDeskOrAdmin")]
public class DetailsModel(FixFlowDbContext database) : PageModel
{
    public Customer Customer { get; private set; } = null!;
    public List<RepairJob> Jobs { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var customer = await database.Customers.Include(item => item.Equipment).SingleOrDefaultAsync(item => item.Id == id);
        if (customer is null)
        {
            return NotFound();
        }

        Customer = customer;
        Jobs = await database.RepairJobs.Include(job => job.Equipment).Where(job => job.CustomerId == id).OrderByDescending(job => job.ReceiveDate).ToListAsync();
        return Page();
    }
}
