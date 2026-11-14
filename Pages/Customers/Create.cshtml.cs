using FixFlow.Data;
using FixFlow.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FixFlow.Pages.Customers;

[Authorize(Policy = "FrontDeskOrAdmin")]
public class CreateModel(FixFlowDbContext database) : PageModel
{
    [BindProperty]
    public Customer Customer { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        Customer.FullName = Customer.FullName.Trim();
        Customer.PhoneNumber = CleanOptionalValue(Customer.PhoneNumber);
        Customer.Email = CleanOptionalValue(Customer.Email);
        Customer.Address = CleanOptionalValue(Customer.Address);
        Customer.Notes = CleanOptionalValue(Customer.Notes);
        database.Customers.Add(Customer);
        await database.SaveChangesAsync();
        TempData["Success"] = "Customer saved.";
        return RedirectToPage("Details", new { id = Customer.Id });
    }

    private static string? CleanOptionalValue(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
