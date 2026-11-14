using FixFlow.Data;
using FixFlow.Models;
using FixFlow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Pages.Payments;

[Authorize(Policy = "AllStaff")]
public class ReceiptModel(FixFlowDbContext database, JobAccessService jobAccessService) : PageModel
{
    public Payment Payment { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var payment = await database.Payments
            .Include(item => item.ReceivedByUser)
            .Include(item => item.RepairJob).ThenInclude(job => job.Customer)
            .Include(item => item.RepairJob).ThenInclude(job => job.Equipment)
            .SingleOrDefaultAsync(item => item.Id == id);
        if (payment is null) return NotFound();
        if (!await jobAccessService.CanAccessJobAsync(User, payment.RepairJob)) return Forbid();
        Payment = payment;
        return Page();
    }
}
