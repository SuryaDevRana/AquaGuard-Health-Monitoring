using System.Security.Claims;
using AquaGuard.Application.Auditing;
using AquaGuard.Domain.Entities;
using AquaGuard.Infrastructure.Persistence;
using AquaGuard.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AquaGuard.Web.Controllers;

[Authorize]
public sealed class SurveillanceController(ApplicationDbContext db, IAuditService audit) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var reports = await db.PatientReports.AsNoTracking()
            .Include(x => x.Patient).Include(x => x.Village).Include(x => x.Disease)
            .OrderByDescending(x => x.ReportedOn).Take(100).ToListAsync(cancellationToken);
        var total = await db.PatientReports.CountAsync(cancellationToken);
        var recent = await db.PatientReports.CountAsync(x => x.ReportedOn >= DateTime.UtcNow.AddDays(-7), cancellationToken);
        var diseases = await db.Diseases.CountAsync(cancellationToken);
        return View(new SurveillanceIndexViewModel(reports, total, recent, diseases));
    }

    [HttpGet]
    public async Task<IActionResult> CreateReport(CancellationToken cancellationToken)
    {
        await LoadReportLookupsAsync(cancellationToken);
        return View(new PatientReportFormViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateReport(PatientReportFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadReportLookupsAsync(cancellationToken, model);
            return View(model);
        }

        var patient = await db.Patients.AsNoTracking().SingleOrDefaultAsync(x => x.Id == model.PatientId, cancellationToken);
        if (patient is null)
        {
            ModelState.AddModelError(nameof(model.PatientId), "Select a valid patient.");
            await LoadReportLookupsAsync(cancellationToken, model);
            return View(model);
        }

        var diseaseExists = await db.Diseases.AnyAsync(x => x.Id == model.DiseaseId, cancellationToken);
        if (!diseaseExists)
        {
            ModelState.AddModelError(nameof(model.DiseaseId), "Select a valid disease.");
            await LoadReportLookupsAsync(cancellationToken, model);
            return View(model);
        }

        var report = new PatientReport
        {
            PatientId = patient.Id,
            VillageId = patient.VillageId,
            DiseaseId = model.DiseaseId,
            Symptoms = model.Symptoms.Trim(),
            ReportedOn = model.ReportedOn.ToUniversalTime(),
            Severity = model.Severity?.Trim(),
            Notes = model.Notes?.Trim(),
            RecordedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
        };
        db.PatientReports.Add(report);
        await db.SaveChangesAsync(cancellationToken);
        await audit.RecordAsync("Created", "PatientReport", report.Id.ToString(), "Patient case report recorded.", cancellationToken);
        TempData["SuccessMessage"] = "Patient report recorded.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> EditReport(int id, CancellationToken cancellationToken)
    {
        var report = await db.PatientReports.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (report is null) return NotFound();
        var model = new PatientReportFormViewModel
        {
            Id = report.Id,
            PatientId = report.PatientId,
            DiseaseId = report.DiseaseId,
            Symptoms = report.Symptoms,
            ReportedOn = report.ReportedOn.ToLocalTime(),
            Severity = report.Severity,
            Notes = report.Notes
        };
        await LoadReportLookupsAsync(cancellationToken, model);
        return View("ReportForm", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditReport(int id, PatientReportFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid)
        {
            await LoadReportLookupsAsync(cancellationToken, model);
            return View("ReportForm", model);
        }

        var report = await db.PatientReports.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (report is null) return NotFound();
        var patient = await db.Patients.AsNoTracking().SingleOrDefaultAsync(x => x.Id == model.PatientId, cancellationToken);
        if (patient is null)
        {
            ModelState.AddModelError(nameof(model.PatientId), "Select a valid patient.");
            await LoadReportLookupsAsync(cancellationToken, model);
            return View("ReportForm", model);
        }
        if (!await db.Diseases.AnyAsync(x => x.Id == model.DiseaseId, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.DiseaseId), "Select a valid disease.");
            await LoadReportLookupsAsync(cancellationToken, model);
            return View("ReportForm", model);
        }

        report.PatientId = patient.Id;
        report.VillageId = patient.VillageId;
        report.DiseaseId = model.DiseaseId;
        report.Symptoms = model.Symptoms.Trim();
        report.ReportedOn = model.ReportedOn.ToUniversalTime();
        report.Severity = model.Severity?.Trim();
        report.Notes = model.Notes?.Trim();
        await db.SaveChangesAsync(cancellationToken);
        await audit.RecordAsync("Updated", "PatientReport", id.ToString(), "Patient case report updated.", cancellationToken);
        TempData["SuccessMessage"] = "Patient report updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteReport(int id, CancellationToken cancellationToken)
    {
        var report = await db.PatientReports.FindAsync([id], cancellationToken);
        if (report is null) return NotFound();
        db.PatientReports.Remove(report);
        await db.SaveChangesAsync(cancellationToken);
        await audit.RecordAsync("Deleted", "PatientReport", id.ToString(), "Patient case report deleted.", cancellationToken);
        TempData["SuccessMessage"] = "Patient report removed.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Diseases(CancellationToken cancellationToken)
    {
        var diseases = await db.Diseases.AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new DiseaseRow(x.Id, x.Name, x.Description, x.IsWaterBorne, x.PatientReports.Count, x.EarlyWarnings.Count))
            .ToListAsync(cancellationToken);
        return View(diseases);
    }

    [HttpGet]
    public IActionResult CreateDisease() => View("DiseaseForm", new DiseaseFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDisease(DiseaseFormViewModel model, CancellationToken cancellationToken)
    {
        if (await db.Diseases.AnyAsync(x => x.Name == model.Name.Trim(), cancellationToken))
            ModelState.AddModelError(nameof(model.Name), "A disease with this name already exists.");
        if (!ModelState.IsValid) return View("DiseaseForm", model);

        var disease = new Disease { Name = model.Name.Trim(), Description = model.Description?.Trim(), IsWaterBorne = model.IsWaterBorne };
        db.Diseases.Add(disease);
        await db.SaveChangesAsync(cancellationToken);
        await audit.RecordAsync("Created", "Disease", disease.Id.ToString(), "Disease catalogue entry created.", cancellationToken);
        TempData["SuccessMessage"] = "Disease added to the catalogue.";
        return RedirectToAction(nameof(Diseases));
    }

    [HttpGet]
    public async Task<IActionResult> EditDisease(int id, CancellationToken cancellationToken)
    {
        var disease = await db.Diseases.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (disease is null) return NotFound();
        return View("DiseaseForm", new DiseaseFormViewModel
        {
            Id = disease.Id,
            Name = disease.Name,
            Description = disease.Description,
            IsWaterBorne = disease.IsWaterBorne
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditDisease(int id, DiseaseFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id) return BadRequest();
        if (await db.Diseases.AnyAsync(x => x.Id != id && x.Name == model.Name.Trim(), cancellationToken))
            ModelState.AddModelError(nameof(model.Name), "A disease with this name already exists.");
        if (!ModelState.IsValid) return View("DiseaseForm", model);

        var disease = await db.Diseases.FindAsync([id], cancellationToken);
        if (disease is null) return NotFound();
        disease.Name = model.Name.Trim();
        disease.Description = model.Description?.Trim();
        disease.IsWaterBorne = model.IsWaterBorne;
        await db.SaveChangesAsync(cancellationToken);
        await audit.RecordAsync("Updated", "Disease", id.ToString(), "Disease catalogue entry updated.", cancellationToken);
        TempData["SuccessMessage"] = "Disease updated.";
        return RedirectToAction(nameof(Diseases));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDisease(int id, CancellationToken cancellationToken)
    {
        var disease = await db.Diseases.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (disease is null) return NotFound();
        if (await db.PatientReports.AnyAsync(x => x.DiseaseId == id, cancellationToken)
            || await db.Patients.AnyAsync(x => x.ReportedDiseaseId == id, cancellationToken)
            || await db.EarlyWarnings.AnyAsync(x => x.RelatedDiseaseId == id, cancellationToken))
        {
            TempData["ErrorMessage"] = "This disease is in use by health records or alerts and cannot be deleted.";
            return RedirectToAction(nameof(Diseases));
        }

        db.Diseases.Remove(disease);
        await db.SaveChangesAsync(cancellationToken);
        await audit.RecordAsync("Deleted", "Disease", id.ToString(), "Disease catalogue entry removed.", cancellationToken);
        TempData["SuccessMessage"] = "Disease removed.";
        return RedirectToAction(nameof(Diseases));
    }

    private async Task LoadReportLookupsAsync(CancellationToken cancellationToken, PatientReportFormViewModel? model = null)
    {
        var patients = await db.Patients.AsNoTracking().Include(x => x.Village)
            .OrderBy(x => x.FullName).ToListAsync(cancellationToken);
        ViewBag.Patients = new SelectList(patients.Select(x =>
            new SelectListItem($"{x.PatientCode} · {x.FullName} — {x.Village.Name}", x.Id.ToString())),
            "Value", "Text", model?.PatientId);
        ViewBag.Diseases = new SelectList(await db.Diseases.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken),
            "Id", "Name", model?.DiseaseId);
    }

    public sealed record DiseaseRow(int Id, string Name, string? Description, bool IsWaterBorne, int CaseCount, int WarningCount);
}
