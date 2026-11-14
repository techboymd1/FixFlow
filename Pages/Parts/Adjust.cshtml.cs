using System.ComponentModel.DataAnnotations;
using FixFlow.Data;
using FixFlow.Models;
using FixFlow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Pages.Parts;

[Authorize(Policy = "AdminOnly")]
public class AdjustModel(FixFlowDbContext database, InventoryService inventoryService) : PageModel
{
    [BindProperty] public Part Part { get; set; } = null!;
    [BindProperty] public AdjustmentInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var part = await database.Parts.SingleOrDefaultAsync(item => item.Id == id);
        if (part is null) return NotFound();
        Part = part;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var part = await database.Parts.SingleOrDefaultAsync(item => item.Id == Part.Id);
        if (part is null) return NotFound();
        Part = part;
        if (!ModelState.IsValid) return Page();
        var result = await inventoryService.AdjustStockAsync(Part.Id, Input.QuantityChange, Input.Reason.Trim(), HttpContext.RequestAborted);
        if (!result.IsSuccessful)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage!);
            return Page();
        }
        TempData["Success"] = "Stock quantity updated.";
        return RedirectToPage("Index");
    }

    public class AdjustmentInput
    {
        [Range(-999999, 999999), Display(Name = "Quantity change")]
        public int QuantityChange { get; set; }
        [Required, StringLength(300)] public string Reason { get; set; } = string.Empty;
    }
}
