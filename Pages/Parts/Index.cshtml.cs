using FixFlow.Data;
using FixFlow.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Pages.Parts;

[Authorize(Policy = "AdminOnly")]
public class IndexModel(FixFlowDbContext database) : PageModel
{
    public string? Search { get; private set; }
    public bool LowStockOnly { get; private set; }
    public List<Part> Parts { get; private set; } = [];

    public async Task OnGetAsync(string? search, bool lowStockOnly)
    {
        Search = search?.Trim();
        LowStockOnly = lowStockOnly;
        var parts = database.Parts.Where(part => part.IsActive).AsQueryable();
        if (!string.IsNullOrWhiteSpace(Search)) parts = parts.Where(part => part.Sku.Contains(Search) || part.Name.Contains(Search));
        if (lowStockOnly) parts = parts.Where(part => part.StockQuantity <= part.ReorderPoint);
        Parts = await parts.OrderBy(part => part.Name).ToListAsync();
    }
}
