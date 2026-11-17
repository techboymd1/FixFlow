using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using FixFlow.Data;
using FixFlow.Models;
using FixFlow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Pages.Staff;

[Authorize(Policy = "AdminOnly")]
public class IndexModel(FixFlowDbContext database, PasswordService passwordService) : PageModel
{
    public List<StaffRow> StaffMembers { get; private set; } = [];

    [BindProperty]
    public CreateStaffInput CreateInput { get; set; } = new();

    public IReadOnlyList<string> Roles => UserRoles.All;

    public async Task OnGetAsync()
    {
        await LoadStaffAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        CreateInput.FullName = (CreateInput.FullName ?? string.Empty).Trim();
        CreateInput.UserName = (CreateInput.UserName ?? string.Empty).Trim();

        if (!UserRoles.All.Contains(CreateInput.Role, StringComparer.Ordinal))
        {
            ModelState.AddModelError("CreateInput.Role", "Select a valid staff role.");
        }

        if (await database.Users.AnyAsync(user => user.UserName == CreateInput.UserName))
        {
            ModelState.AddModelError("CreateInput.UserName", "That username is already in use.");
        }

        if (!ModelState.IsValid)
        {
            await LoadStaffAsync();
            return Page();
        }

        database.Users.Add(new AppUser
        {
            FullName = CreateInput.FullName,
            UserName = CreateInput.UserName,
            PasswordHash = passwordService.HashPassword(CreateInput.Password),
            Role = CreateInput.Role,
            IsActive = true
        });

        await database.SaveChangesAsync();
        TempData["Success"] = $"Staff account '{CreateInput.UserName}' was created.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleActiveAsync(int id)
    {
        var staff = await database.Users.SingleOrDefaultAsync(user => user.Id == id);
        if (staff is null)
            return NotFound();

        var currentUserId = int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out var parsedUserId
        ) ? parsedUserId : -1;

        if (staff.IsActive && staff.Id == currentUserId)
        {
            TempData["Error"] = "You cannot deactivate your own account.";
            return RedirectToPage();
        }

        if (staff.IsActive && staff.Role == UserRoles.OwnerAdmin)
        {
            var activeOwnerCount = await database.Users.CountAsync(user =>
                user.IsActive && user.Role == UserRoles.OwnerAdmin
            );

            if (activeOwnerCount <= 1)
            {
                TempData["Error"] = "The last active Owner/Admin account cannot be deactivated.";
                return RedirectToPage();
            }
        }

        staff.IsActive = !staff.IsActive;
        await database.SaveChangesAsync();

        TempData["Success"] = staff.IsActive
            ? $"'{staff.UserName}' was reactivated."
            : $"'{staff.UserName}' was deactivated.";

        return RedirectToPage();
    }

    private async Task LoadStaffAsync()
    {
        StaffMembers = await database.Users
            .AsNoTracking()
            .OrderBy(user => user.Role)
            .ThenBy(user => user.FullName)
            .Select(user => new StaffRow(
                user.Id,
                user.FullName,
                user.UserName,
                user.Role,
                user.IsActive
            ))
            .ToListAsync();
    }

    public sealed record StaffRow(
        int Id,
        string FullName,
        string UserName,
        string Role,
        bool IsActive
    );

    public class CreateStaffInput
    {
        [Required, StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, StringLength(80, MinimumLength = 3)]
        [RegularExpression("^[A-Za-z0-9._-]+$", ErrorMessage = "Use letters, numbers, dots, underscores or hyphens for the username.")]
        public string UserName { get; set; } = string.Empty;

        [Required, StringLength(30)]
        public string Role { get; set; } = UserRoles.Technician;

        [Required, MinLength(12), DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required, Compare(nameof(Password)), DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
