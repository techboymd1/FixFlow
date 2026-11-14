using FixFlow.Data;
using FixFlow.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Pages.Equipment;

[Authorize(Policy = "FrontDeskOrAdmin")]
public class IndexModel(FixFlowDbContext database) : PageModel
{
    public string? Search { get; private set; }
    public List<FixFlow.Models.Equipment> EquipmentItems { get; private set; } = [];

    public async Task OnGetAsync(string? search)
    {
        Search = search?.Trim();
        var equipment = database.Equipment.Include(item => item.Customer).AsQueryable();
        if (!string.IsNullOrWhiteSpace(Search))
        {
            equipment = equipment.Where(item =>
                item.Customer.FullName.Contains(Search) ||
                (item.Brand != null && item.Brand.Contains(Search)) ||
                (item.Model != null && item.Model.Contains(Search)) ||
                (item.SerialNumber != null && item.SerialNumber.Contains(Search)) ||
                item.Category.Contains(Search));
        }
        EquipmentItems = await equipment.OrderByDescending(item => item.CreatedAtUtc).ToListAsync();
    }
}
