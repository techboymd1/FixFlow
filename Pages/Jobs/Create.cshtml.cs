using System.ComponentModel.DataAnnotations;
using FixFlow.Data;
using FixFlow.Models;
using FixFlow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Pages.Jobs;

[Authorize(Policy = "FrontDeskOrAdmin")]
public class CreateModel(FixFlowDbContext database, JobReferenceService jobReferenceService, JobAccessService jobAccessService) : PageModel
{
    [BindProperty]
    public JobInput Input { get; set; } = new();

    public List<SelectListItem> EquipmentOptions { get; private set; } = [];

    public async Task OnGetAsync(int? equipmentId)
    {
        Input.EquipmentId = equipmentId ?? 0;
        await LoadEquipmentAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var equipment = await database.Equipment.SingleOrDefaultAsync(item => item.Id == Input.EquipmentId);
        if (equipment is null) ModelState.AddModelError("Input.EquipmentId", "Select registered customer equipment.");
        if (!ModelState.IsValid)
        {
            await LoadEquipmentAsync();
            return Page();
        }

        var job = new RepairJob
        {
            JobReference = await jobReferenceService.CreateNextReferenceAsync(Input.ReceiveDate),
            CustomerId = equipment!.CustomerId,
            EquipmentId = equipment.Id,
            ReceiveDate = Input.ReceiveDate,
            ExpectedCompletionDate = Input.ExpectedCompletionDate,
            ReportedFault = Input.ReportedFault.Trim(),
            ConditionAndAccessories = Clean(Input.ConditionAndAccessories),
            InternalNotes = Clean(Input.InternalNotes),
            Status = RepairJobStatus.Received
        };
        database.RepairJobs.Add(job);
        await database.SaveChangesAsync();
        var userId = jobAccessService.GetCurrentUserId(User);
        database.JobStatusHistories.Add(new JobStatusHistory { RepairJobId = job.Id, FromStatus = RepairJobStatus.Received, ToStatus = RepairJobStatus.Received, ChangedByUserId = userId, Notes = "Job card created." });
        await database.SaveChangesAsync();
        TempData["Success"] = $"Job {job.JobReference} was created.";
        return RedirectToPage("Details", new { id = job.Id });
    }

    private async Task LoadEquipmentAsync()
    {
        EquipmentOptions = await database.Equipment.Include(item => item.Customer).OrderBy(item => item.Customer.FullName).ThenBy(item => item.Category)
            .Select(item => new SelectListItem($"{item.Customer.FullName} — {item.Category} {item.Brand} {item.Model}", item.Id.ToString()))
            .ToListAsync();
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public class JobInput
    {
        [Range(1, int.MaxValue, ErrorMessage = "Select customer equipment.")]
        public int EquipmentId { get; set; }
        [Required, DataType(DataType.Date), Display(Name = "Receive date")]
        public DateTime ReceiveDate { get; set; } = DateTime.Today;
        [DataType(DataType.Date), Display(Name = "Expected completion date")]
        public DateTime? ExpectedCompletionDate { get; set; }
        [Required, StringLength(1500), Display(Name = "Reported fault")]
        public string ReportedFault { get; set; } = string.Empty;
        [StringLength(1500), Display(Name = "Condition and accessories received")]
        public string? ConditionAndAccessories { get; set; }
        [StringLength(1000), Display(Name = "Internal intake notes")]
        public string? InternalNotes { get; set; }
    }
}
