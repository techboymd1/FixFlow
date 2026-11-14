using FixFlow.Data;
using FixFlow.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Pages.Parts;

[Authorize(Policy = "AdminOnly")]
public class CreateModel(FixFlowDbContext database) : PageModel
{
    [BindProperty] public Part Part { get; set; } = new();
    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (await database.Parts.AnyAsync(item => item.Sku == Part.Sku.Trim())) ModelState.AddModelError("Part.Sku", "That SKU already exists.");
        if (!ModelState.IsValid) return Page();
        Part.Sku = Part.Sku.Trim().ToUpperInvariant();
        Part.Name = Part.Name.Trim();
        Part.Description = string.IsNullOrWhiteSpace(Part.Description) ? null : Part.Description.Trim();
        database.Parts.Add(Part);
        if (Part.StockQuantity > 0) database.StockMovements.Add(new StockMovement { Part = Part, QuantityChange = Part.StockQuantity, MovementType = "Opening stock", Reason = "Part created" });
        await database.SaveChangesAsync();
        TempData["Success"] = "Part saved.";
        return RedirectToPage("Index");
    }
}
