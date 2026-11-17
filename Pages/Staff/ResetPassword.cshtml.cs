using System.ComponentModel.DataAnnotations;
using FixFlow.Data;
using FixFlow.Models;
using FixFlow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Pages.Staff;

[Authorize(Policy = "AdminOnly")]
public class ResetPasswordModel(FixFlowDbContext database, PasswordService passwordService) : PageModel
{
    public AppUser StaffMember { get; private set; } = null!;

    [BindProperty]
    public PasswordInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var result = await LoadStaffMemberAsync(id);
        return result is null ? Page() : result;
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var result = await LoadStaffMemberAsync(id);
        if (result is not null)
            return result;

        if (!ModelState.IsValid)
            return Page();

        StaffMember.PasswordHash = passwordService.HashPassword(Input.Password);
        await database.SaveChangesAsync();

        TempData["Success"] = $"Password updated for '{StaffMember.UserName}'.";
        return RedirectToPage("Index");
    }

    private async Task<IActionResult?> LoadStaffMemberAsync(int id)
    {
        var staff = await database.Users.SingleOrDefaultAsync(user => user.Id == id);
        if (staff is null)
            return NotFound();

        StaffMember = staff;
        return null;
    }

    public class PasswordInput
    {
        [Required, MinLength(12), DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required, Compare(nameof(Password)), DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
