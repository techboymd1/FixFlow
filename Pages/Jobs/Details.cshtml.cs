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

[Authorize(Policy = "AllStaff")]
public class DetailsModel(
    FixFlowDbContext database,
    JobAccessService jobAccessService,
    JobWorkflowService workflowService,
    InventoryService inventoryService,
    PaymentService paymentService) : PageModel
{
    public RepairJob Job { get; private set; } = null!;
    public List<RepairJobStatus> AvailableStatuses { get; private set; } = [];
    public List<SelectListItem> TechnicianOptions { get; private set; } = [];
    public List<SelectListItem> PartOptions { get; private set; } = [];
    public decimal JobTotal { get; private set; }
    public decimal OutstandingBalance { get; private set; }

    [BindProperty] public StatusInputModel StatusInput { get; set; } = new();
    [BindProperty] public EstimateInputModel EstimateInput { get; set; } = new();
    [BindProperty] public NoteInputModel NoteInput { get; set; } = new();
    [BindProperty] public PartInputModel PartInput { get; set; } = new();
    [BindProperty] public PaymentInputModel PaymentInput { get; set; } = new();
    [BindProperty] public AssignmentInputModel AssignmentInput { get; set; } = new();
    [BindProperty] public ApprovalInputModel ApprovalInput { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var result = await LoadAuthorizedJobAsync(id);
        if (result is not null) return result;
        AssignmentInput.TechnicianId = Job.TechnicianId;
        await LoadOptionsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostTransitionAsync(int id)
    {
        var result = await LoadAuthorizedJobAsync(id);
        if (result is not null) return result;
        if (!Enum.TryParse<RepairJobStatus>(StatusInput.NextStatus, out var nextStatus))
        {
            TempData["Error"] = "Select a valid next status.";
            return RedirectToPage(new { id });
        }
        var transition = await workflowService.TransitionAsync(Job, nextStatus, jobAccessService.GetCurrentUserId(User), StatusInput.Notes, HttpContext.RequestAborted);
        TempData[transition.IsSuccessful ? "Success" : "Error"] = transition.IsSuccessful ? "Job status updated." : transition.ErrorMessage!;
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAddNoteAsync(int id)
    {
        var result = await LoadAuthorizedJobAsync(id);
        if (result is not null) return result;
        if (string.IsNullOrWhiteSpace(NoteInput.NoteText))
        {
            TempData["Error"] = "Write a note before saving it.";
            return RedirectToPage(new { id });
        }
        database.JobNotes.Add(new JobNote { RepairJobId = id, CreatedByUserId = jobAccessService.GetCurrentUserId(User), NoteText = NoteInput.NoteText.Trim(), IsDiagnosis = NoteInput.IsDiagnosis });
        Job.UpdatedAtUtc = DateTime.UtcNow;
        await database.SaveChangesAsync();
        TempData["Success"] = "Job note added.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostSaveEstimateAsync(int id)
    {
        if (!CanManageOffice()) return Forbid();
        var result = await LoadAuthorizedJobAsync(id);
        if (result is not null) return result;
        if (EstimateInput.PartsAmount < 0 || EstimateInput.LabourAmount < 0)
        {
            TempData["Error"] = "Estimate amounts cannot be negative.";
            return RedirectToPage(new { id });
        }
        if (Job.Estimate is null)
        {
            Job.Estimate = new RepairEstimate { RepairJobId = id };
            database.RepairEstimates.Add(Job.Estimate);
        }
        Job.Estimate.PartsAmount = EstimateInput.PartsAmount;
        Job.Estimate.LabourAmount = EstimateInput.LabourAmount;
        Job.Estimate.Description = Clean(EstimateInput.Description);
        Job.Estimate.ApprovalStatus = ApprovalStatus.Pending;
        Job.Estimate.RespondedAtUtc = null;
        Job.Estimate.ResponseNotes = null;
        Job.UpdatedAtUtc = DateTime.UtcNow;
        await database.SaveChangesAsync();
        TempData["Success"] = "Estimate saved and marked pending approval.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRespondEstimateAsync(int id)
    {
        if (!CanManageOffice()) return Forbid();
        var result = await LoadAuthorizedJobAsync(id);
        if (result is not null) return result;
        if (Job.Estimate is null || !Enum.TryParse<ApprovalStatus>(ApprovalInput.Decision, out var decision) || decision == ApprovalStatus.Pending)
        {
            TempData["Error"] = "An estimate and a valid response are required.";
            return RedirectToPage(new { id });
        }
        Job.Estimate.ApprovalStatus = decision;
        Job.Estimate.RespondedAtUtc = DateTime.UtcNow;
        Job.Estimate.ResponseNotes = Clean(ApprovalInput.ResponseNotes);
        Job.UpdatedAtUtc = DateTime.UtcNow;
        await database.SaveChangesAsync();
        TempData["Success"] = $"Estimate {decision.ToString().ToLowerInvariant()} recorded.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAddPartAsync(int id)
    {
        var result = await LoadAuthorizedJobAsync(id);
        if (result is not null) return result;
        if (!CanManageInventory() || Job.Status != RepairJobStatus.InProgress)
        {
            return Forbid();
        }
        var inventoryResult = await inventoryService.UsePartOnJobAsync(id, PartInput.PartId, PartInput.Quantity, jobAccessService.GetCurrentUserId(User), HttpContext.RequestAborted);
        TempData[inventoryResult.IsSuccessful ? "Success" : "Error"] = inventoryResult.IsSuccessful ? "Part usage recorded and stock updated." : inventoryResult.ErrorMessage!;
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRecordPaymentAsync(int id)
    {
        if (!CanManageOffice()) return Forbid();
        var result = await LoadAuthorizedJobAsync(id);
        if (result is not null) return result;
        if (PaymentInput.PaymentMethod is not ("Cash" or "Bank transfer"))
        {
            TempData["Error"] = "Select an allowed payment method.";
            return RedirectToPage(new { id });
        }
        var paymentResult = await paymentService.RecordPaymentAsync(id, PaymentInput.Amount, PaymentInput.PaymentMethod, Clean(PaymentInput.Notes), jobAccessService.GetCurrentUserId(User), HttpContext.RequestAborted);
        TempData[paymentResult.IsSuccessful ? "Success" : "Error"] = paymentResult.IsSuccessful ? "Payment recorded." : paymentResult.ErrorMessage!;
        return paymentResult.IsSuccessful
            ? RedirectToPage("/Payments/Receipt", new { id = paymentResult.PaymentId })
            : RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAssignTechnicianAsync(int id)
    {
        if (!User.IsInRole(UserRoles.OwnerAdmin)) return Forbid();
        var result = await LoadAuthorizedJobAsync(id);
        if (result is not null) return result;
        if (AssignmentInput.TechnicianId.HasValue && !await database.Users.AnyAsync(user => user.Id == AssignmentInput.TechnicianId && user.Role == UserRoles.Technician && user.IsActive))
        {
            TempData["Error"] = "Select an active technician.";
            return RedirectToPage(new { id });
        }
        Job.TechnicianId = AssignmentInput.TechnicianId;
        Job.UpdatedAtUtc = DateTime.UtcNow;
        await database.SaveChangesAsync();
        TempData["Success"] = "Technician assignment updated.";
        return RedirectToPage(new { id });
    }

    private async Task<IActionResult?> LoadAuthorizedJobAsync(int id)
    {
        var job = await database.RepairJobs
            .Include(item => item.Customer).Include(item => item.Equipment).Include(item => item.Technician)
            .Include(item => item.Estimate)
            .Include(item => item.Notes).ThenInclude(note => note.CreatedByUser)
            .Include(item => item.PartsUsed).ThenInclude(part => part.Part)
            .Include(item => item.Payments).ThenInclude(payment => payment.ReceivedByUser)
            .Include(item => item.StatusHistory).ThenInclude(history => history.ChangedByUser)
            .SingleOrDefaultAsync(item => item.Id == id);
        if (job is null) return NotFound();
        if (!await jobAccessService.CanAccessJobAsync(User, job)) return Forbid();
        Job = job;
        AvailableStatuses = workflowService.GetAllowedNextStatuses(job.Status).ToList();
        JobTotal = PaymentService.GetJobTotal(job);
        OutstandingBalance = JobTotal - job.Payments.Sum(payment => payment.Amount);
        return null;
    }

    private async Task LoadOptionsAsync()
    {
        if (User.IsInRole(UserRoles.OwnerAdmin))
        {
            TechnicianOptions = await database.Users.Where(user => user.IsActive && user.Role == UserRoles.Technician)
                .OrderBy(user => user.FullName).Select(user => new SelectListItem(user.FullName, user.Id.ToString())).ToListAsync();
        }
        if (CanManageInventory())
        {
            PartOptions = await database.Parts.Where(part => part.IsActive && part.StockQuantity > 0)
                .OrderBy(part => part.Name).Select(part => new SelectListItem($"{part.Name} ({part.StockQuantity} available)", part.Id.ToString())).ToListAsync();
        }
    }

    private bool CanManageOffice() => User.IsInRole(UserRoles.OwnerAdmin) || User.IsInRole(UserRoles.FrontDesk);
    private bool CanManageInventory() => User.IsInRole(UserRoles.OwnerAdmin) || User.IsInRole(UserRoles.Technician);
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public class StatusInputModel { public string NextStatus { get; set; } = string.Empty; [StringLength(500)] public string? Notes { get; set; } }
    public class EstimateInputModel { public decimal PartsAmount { get; set; } public decimal LabourAmount { get; set; } [StringLength(1200)] public string? Description { get; set; } }
    public class NoteInputModel { [StringLength(2000)] public string? NoteText { get; set; } public bool IsDiagnosis { get; set; } }
    public class PartInputModel { public int PartId { get; set; } public int Quantity { get; set; } }
    public class PaymentInputModel { public decimal Amount { get; set; } public string PaymentMethod { get; set; } = "Cash"; [StringLength(250)] public string? Notes { get; set; } }
    public class AssignmentInputModel { public int? TechnicianId { get; set; } }
    public class ApprovalInputModel { public string Decision { get; set; } = "Approved"; [StringLength(500)] public string? ResponseNotes { get; set; } }
}
