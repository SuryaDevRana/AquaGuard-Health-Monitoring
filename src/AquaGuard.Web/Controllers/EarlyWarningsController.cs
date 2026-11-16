using System.Security.Claims;
using AquaGuard.Application.Auditing;
using AquaGuard.Application.EarlyWarnings;
using AquaGuard.Application.Patients;
using AquaGuard.Domain.Entities;
using AquaGuard.Domain.Enums;
using AquaGuard.Infrastructure.Persistence;
using AquaGuard.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AquaGuard.Web.Controllers;

[Authorize]
public sealed class EarlyWarningsController(ApplicationDbContext db, IEarlyWarningService warningService, IAuditService audit) : Controller
{
    public async Task<IActionResult> Index(
        string? search,
        WarningStatus? status,
        RiskLevel? riskLevel,
        int? villageId,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid
            || (status.HasValue && !Enum.IsDefined(status.Value))
            || (riskLevel.HasValue && !Enum.IsDefined(riskLevel.Value)))
            return BadRequest();

        var term = search?.Trim();
        if (term?.Length > 100) term = term[..100];
        var scopedQuery = db.EarlyWarnings.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(term))
            scopedQuery = scopedQuery.Where(x => x.AlertCode.Contains(term) || x.Village.Name.Contains(term) || x.TriggerReason.Contains(term));
        if (riskLevel.HasValue) scopedQuery = scopedQuery.Where(x => x.RiskLevel == riskLevel.Value);
        if (villageId.HasValue) scopedQuery = scopedQuery.Where(x => x.VillageId == villageId.Value);

        var matchingOpenCount = await scopedQuery.CountAsync(x => x.Status == WarningStatus.Open, cancellationToken);
        var matchingAcknowledgedCount = await scopedQuery.CountAsync(x => x.Status == WarningStatus.Acknowledged, cancellationToken);
        var query = status.HasValue ? scopedQuery.Where(x => x.Status == status.Value) : scopedQuery;
        var matchingCount = await query.CountAsync(cancellationToken);
        var warnings = await query.Include(x => x.Village).Include(x => x.RelatedDisease)
            .OrderBy(x => x.Status == WarningStatus.Open ? 0 : x.Status == WarningStatus.Acknowledged ? 1 : 2)
            .ThenByDescending(x => x.RiskLevel == RiskLevel.Critical ? 3 : x.RiskLevel == RiskLevel.High ? 2 : x.RiskLevel == RiskLevel.Moderate ? 1 : 0)
            .ThenByDescending(x => x.AlertDate).Take(150).ToListAsync(cancellationToken);
        var openCount = await db.EarlyWarnings.CountAsync(x => x.Status == WarningStatus.Open, cancellationToken);
        var acknowledgedCount = await db.EarlyWarnings.CountAsync(x => x.Status == WarningStatus.Acknowledged, cancellationToken);
        var activeCount = openCount + acknowledgedCount;
        var criticalCount = await db.EarlyWarnings.CountAsync(x => x.RiskLevel == RiskLevel.Critical
            && (x.Status == WarningStatus.Open || x.Status == WarningStatus.Acknowledged), cancellationToken);
        var villages = await db.Villages.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new LookupItem(x.Id, x.Name)).ToListAsync(cancellationToken);

        return View(new WarningsIndexViewModel(
            warnings,
            matchingCount,
            activeCount,
            openCount,
            acknowledgedCount,
            criticalCount,
            matchingOpenCount,
            matchingAcknowledgedCount,
            term,
            status,
            riskLevel,
            villageId,
            villages));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var warning = await db.EarlyWarnings.AsNoTracking().Include(x => x.Village).Include(x => x.RelatedDisease)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return warning is null ? NotFound() : View(warning);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        await LoadWarningLookupsAsync(null, null, cancellationToken);
        return View(new ManualWarningFormViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ManualWarningFormViewModel model, CancellationToken cancellationToken)
    {
        if (!model.RiskLevel.HasValue || !Enum.IsDefined(model.RiskLevel.Value))
            ModelState.AddModelError(nameof(model.RiskLevel), "Select a valid risk level.");

        if (!ModelState.IsValid)
        {
            await LoadWarningLookupsAsync(model.VillageId, model.RelatedDiseaseId, cancellationToken);
            return View(model);
        }

        var village = await db.Villages.AsNoTracking().SingleOrDefaultAsync(x => x.Id == model.VillageId, cancellationToken);
        if (village is null)
            ModelState.AddModelError(nameof(model.VillageId), "Select a valid village.");
        if (model.RelatedDiseaseId.HasValue
            && !await db.Diseases.AnyAsync(x => x.Id == model.RelatedDiseaseId.Value, cancellationToken))
            ModelState.AddModelError(nameof(model.RelatedDiseaseId), "Select a valid disease.");

        var recipientIds = await db.Users.AsNoTracking().Where(x => x.IsActive).Select(x => x.Id).ToListAsync(cancellationToken);
        if (recipientIds.Count == 0)
            ModelState.AddModelError(string.Empty, "There are no active user inboxes to notify.");
        if (!ModelState.IsValid || village is null)
        {
            await LoadWarningLookupsAsync(model.VillageId, model.RelatedDiseaseId, cancellationToken);
            return View(model);
        }

        var warning = new EarlyWarning
        {
            AlertCode = $"AGW-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            VillageId = village.Id,
            RiskLevel = model.RiskLevel!.Value,
            TriggerReason = model.TriggerReason.Trim(),
            RelatedDiseaseId = model.RelatedDiseaseId,
            ReportedCaseCount = model.ReportedCaseCount,
            AlertDate = DateTime.UtcNow,
            RecommendedAction = model.RecommendedAction.Trim(),
            EvidenceSummary = string.IsNullOrWhiteSpace(model.EvidenceSummary)
                ? "Manually submitted by an authenticated user for human review."
                : $"Manually submitted for human review. {model.EvidenceSummary.Trim()}",
            Status = WarningStatus.Open
        };

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.EarlyWarnings.Add(warning);
        await db.SaveChangesAsync(cancellationToken);

        var detailsUrl = Url.Action(nameof(Details), "EarlyWarnings", new { id = warning.Id }) ?? "/EarlyWarnings";
        foreach (var userId in recipientIds)
        {
            db.Notifications.Add(new Notification
            {
                UserId = userId,
                Type = NotificationType.EarlyWarning,
                Title = $"{warning.RiskLevel} warning for {village.Name}",
                Message = warning.TriggerReason,
                Link = detailsUrl
            });
        }
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await audit.RecordAsync("Created manually", "EarlyWarning", warning.Id.ToString(),
            $"Manual {warning.RiskLevel} warning sent to {recipientIds.Count} active user inbox(es).", cancellationToken);
        TempData["SuccessMessage"] = $"Warning sent to {recipientIds.Count} active user inbox(es).";
        return RedirectToAction(nameof(Details), new { id = warning.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Evaluate(CancellationToken cancellationToken)
    {
        var created = await warningService.EvaluateAndCreateWarningsAsync(cancellationToken);
        await audit.RecordAsync("Evaluated", "EarlyWarning", null, $"{created} warning(s) generated.", cancellationToken);
        TempData["SuccessMessage"] = created == 0
            ? "Review complete. No new rule-based warnings were generated."
            : $"Review complete. {created} new warning(s) are ready for human review.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int id, WarningStatus status, string? returnUrl, CancellationToken cancellationToken)
    {
        if (status is not (WarningStatus.Acknowledged or WarningStatus.Resolved or WarningStatus.Dismissed))
            return BadRequest();
        var warning = await db.EarlyWarnings.FindAsync([id], cancellationToken);
        if (warning is null) return NotFound();
        if (warning.Status is WarningStatus.Resolved or WarningStatus.Dismissed)
        {
            TempData["ErrorMessage"] = "This warning is already closed and cannot be changed.";
            return RedirectAfterReview(returnUrl, id);
        }

        warning.Status = status;
        if (status == WarningStatus.Acknowledged)
            warning.AcknowledgedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (status is WarningStatus.Resolved or WarningStatus.Dismissed)
            warning.ResolvedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await audit.RecordAsync("Status changed", "EarlyWarning", id.ToString(), $"Warning marked {status}.", cancellationToken);
        TempData["SuccessMessage"] = $"Warning marked {status.ToString().ToLowerInvariant()}.";
        return RedirectAfterReview(returnUrl, id);
    }

    private IActionResult RedirectAfterReview(string? returnUrl, int id) =>
        !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Details), new { id });

    private async Task LoadWarningLookupsAsync(int? selectedVillageId, int? selectedDiseaseId, CancellationToken cancellationToken)
    {
        ViewBag.Villages = new SelectList(
            await db.Villages.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken),
            "Id", "Name", selectedVillageId);
        ViewBag.Diseases = new SelectList(
            await db.Diseases.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken),
            "Id", "Name", selectedDiseaseId);
    }
}
