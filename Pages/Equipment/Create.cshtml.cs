using FixFlow.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Pages.Equipment;

[Authorize(Policy = "FrontDeskOrAdmin")]
public class CreateModel(FixFlowDbContext database) : PageModel
{
    [BindProperty]
    public FixFlow.Models.Equipment Equipment { get; set; } = new();

    public List<SelectListItem> CustomerOptions { get; private set; } = [];

    public async Task OnGetAsync(int? customerId)
    {
        Equipment.CustomerId = customerId ?? 0;
        await LoadCustomersAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await database.Customers.AnyAsync(customer => customer.Id == Equipment.CustomerId))
        {
            ModelState.AddModelError("Equipment.CustomerId", "Select a valid customer.");
        }
        if (!ModelState.IsValid)
        {
            await LoadCustomersAsync();
            return Page();
        }

        Equipment.Category = Equipment.Category.Trim();
        Equipment.Brand = Clean(Equipment.Brand);
        Equipment.Model = Clean(Equipment.Model);
        Equipment.SerialNumber = Clean(Equipment.SerialNumber);
        Equipment.Capacity = Clean(Equipment.Capacity);
        Equipment.Notes = Clean(Equipment.Notes);
        database.Equipment.Add(Equipment);
        await database.SaveChangesAsync();
        TempData["Success"] = "Equipment saved.";
        return RedirectToPage("Details", new { id = Equipment.Id });
    }

    private async Task LoadCustomersAsync()
    {
        CustomerOptions = await database
            .Customers.OrderBy(customer => customer.FullName)
            .Select(customer => new SelectListItem(customer.FullName, customer.Id.ToString()))
            .ToListAsync();
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
