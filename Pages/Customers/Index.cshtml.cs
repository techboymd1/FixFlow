using FixFlow.Data;
using FixFlow.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Pages.Customers;

[Authorize(Policy = "FrontDeskOrAdmin")]
public class IndexModel(FixFlowDbContext database) : PageModel
{
    public string? Search { get; private set; }
    public List<Customer> Customers { get; private set; } = [];

    public async Task OnGetAsync(string? search)
    {
        Search = search?.Trim();
        var customers = database.Customers.Include(customer => customer.Equipment).AsQueryable();
        if (!string.IsNullOrWhiteSpace(Search))
        {
            customers = customers.Where(customer =>
                customer.FullName.Contains(Search) ||
                (customer.PhoneNumber != null && customer.PhoneNumber.Contains(Search)) ||
                (customer.Email != null && customer.Email.Contains(Search)));
        }

        Customers = await customers.OrderBy(customer => customer.FullName).ToListAsync();
    }
}
