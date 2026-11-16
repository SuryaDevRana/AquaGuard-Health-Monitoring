using AquaGuard.Domain.Entities;
using AquaGuard.Domain.Enums;
using AquaGuard.Application.Auditing;
using AquaGuard.Infrastructure.Persistence;
using AquaGuard.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AquaGuard.Web.Controllers;

[Authorize]
public sealed class WaterQualityController(ApplicationDbContext db, IAuditService audit) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var reports = await db.WaterQualityReports.AsNoTracking()
            .Include(x => x.WaterSource).ThenInclude(x => x.Village)
            .Include(x => x.TestedByHealthWorker)
            .OrderByDescending(x => x.TestingDate).Take(100).ToListAsync(cancellationToken);
        var counts = await db.WaterQualityReports.AsNoTracking().GroupBy(x => x.Status)
            .Select(x => new { Status = x.Key, Count = x.Count() }).ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);
        return View(new WaterQualityIndexViewModel(
            reports,
            counts.GetValueOrDefault(QualityStatus.Safe),
            counts.GetValueOrDefault(QualityStatus.AttentionNeeded),
            counts.GetValueOrDefault(QualityStatus.Pending),
            counts.GetValueOrDefault(QualityStatus.Unsafe)));
    }

    [HttpGet]
    public async Task<IActionResult> CreateReport(CancellationToken cancellationToken)
    {
        await LoadReportLookupsAsync(cancellationToken);
        return View("ReportForm", new WaterQualityReportFormViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateReport(WaterQualityReportFormViewModel model, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(model.EColiResult) || !Enum.IsDefined(model.TotalColiformResult) || !Enum.IsDefined(model.Status))
            ModelState.AddModelError(string.Empty, "Select valid test results and a report status.");
        if (!ModelState.IsValid)
        {
            await LoadReportLookupsAsync(cancellationToken, model);
            return View("ReportForm", model);
        }

        if (!await db.WaterSources.AnyAsync(x => x.Id == model.WaterSourceId, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.WaterSourceId), "Select a valid water source.");
            await LoadReportLookupsAsync(cancellationToken, model);
            return View("ReportForm", model);
        }

        if (model.TestedByHealthWorkerId.HasValue
            && !await db.HealthWorkers.AnyAsync(x => x.Id == model.TestedByHealthWorkerId, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.TestedByHealthWorkerId), "Select a valid health worker.");
            await LoadReportLookupsAsync(cancellationToken, model);
            return View("ReportForm", model);
        }

        var report = ToReport(model);
        db.WaterQualityReports.Add(report);
        await db.SaveChangesAsync(cancellationToken);
        await audit.RecordAsync("Created", "WaterQualityReport", report.Id.ToString(), "Water-quality report recorded.", cancellationToken);
        TempData["SuccessMessage"] = "Water-quality report recorded.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> EditReport(int id, CancellationToken cancellationToken)
    {
        var report = await db.WaterQualityReports.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (report is null) return NotFound();
        var model = new WaterQualityReportFormViewModel
        {
            Id = report.Id,
            WaterSourceId = report.WaterSourceId,
            PhLevel = report.PhLevel,
            TurbidityNtu = report.TurbidityNtu,
            EColiResult = report.EColiResult,
            TotalColiformResult = report.TotalColiformResult,
            IsEstimated = report.IsEstimated,
            TestingDate = report.TestingDate,
            TestedByHealthWorkerId = report.TestedByHealthWorkerId,
            Remarks = report.Remarks,
            Status = report.Status
        };
        await LoadReportLookupsAsync(cancellationToken, model);
        return View("ReportForm", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditReport(int id, WaterQualityReportFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id) return BadRequest();
        if (!Enum.IsDefined(model.EColiResult) || !Enum.IsDefined(model.TotalColiformResult) || !Enum.IsDefined(model.Status))
            ModelState.AddModelError(string.Empty, "Select valid test results and a report status.");
        if (!ModelState.IsValid)
        {
            await LoadReportLookupsAsync(cancellationToken, model);
            return View("ReportForm", model);
        }

        var report = await db.WaterQualityReports.FindAsync([id], cancellationToken);
        if (report is null) return NotFound();
        if (!await db.WaterSources.AnyAsync(x => x.Id == model.WaterSourceId, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.WaterSourceId), "Select a valid water source.");
            await LoadReportLookupsAsync(cancellationToken, model);
            return View("ReportForm", model);
        }
        if (model.TestedByHealthWorkerId.HasValue
            && !await db.HealthWorkers.AnyAsync(x => x.Id == model.TestedByHealthWorkerId, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.TestedByHealthWorkerId), "Select a valid health worker.");
            await LoadReportLookupsAsync(cancellationToken, model);
            return View("ReportForm", model);
        }
        ApplyReport(report, model);
        await db.SaveChangesAsync(cancellationToken);
        await audit.RecordAsync("Updated", "WaterQualityReport", id.ToString(), "Water-quality report updated.", cancellationToken);
        TempData["SuccessMessage"] = "Water-quality report updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteReport(int id, CancellationToken cancellationToken)
    {
        var report = await db.WaterQualityReports.FindAsync([id], cancellationToken);
        if (report is null) return NotFound();
        db.WaterQualityReports.Remove(report);
        await db.SaveChangesAsync(cancellationToken);
        await audit.RecordAsync("Deleted", "WaterQualityReport", id.ToString(), "Water-quality report deleted.", cancellationToken);
        TempData["SuccessMessage"] = "Water-quality report removed.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Sources(CancellationToken cancellationToken)
    {
        var sources = await db.WaterSources.AsNoTracking().Include(x => x.Village).Include(x => x.QualityReports)
            .OrderBy(x => x.Village.Name).ThenBy(x => x.Name).ToListAsync(cancellationToken);
        return View(new WaterSourcesViewModel(sources));
    }

    [HttpGet]
    public async Task<IActionResult> CreateSource(CancellationToken cancellationToken)
    {
        await LoadVillageLookupAsync(cancellationToken);
        return View("SourceForm", new WaterSourceFormViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSource(WaterSourceFormViewModel model, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(model.SourceType))
            ModelState.AddModelError(nameof(model.SourceType), "Select a valid water source type.");
        if (!ModelState.IsValid)
        {
            await LoadVillageLookupAsync(cancellationToken, model.VillageId);
            return View("SourceForm", model);
        }
        if (await db.WaterSources.AnyAsync(x => x.Name == model.Name.Trim() && x.VillageId == model.VillageId, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Name), "A water source with this name already exists in the selected village.");
            await LoadVillageLookupAsync(cancellationToken, model.VillageId);
            return View("SourceForm", model);
        }
        if (!await db.Villages.AnyAsync(x => x.Id == model.VillageId, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.VillageId), "Select a valid village.");
            await LoadVillageLookupAsync(cancellationToken, model.VillageId);
            return View("SourceForm", model);
        }

        var source = ToSource(model);
        db.WaterSources.Add(source);
        await db.SaveChangesAsync(cancellationToken);
        await audit.RecordAsync("Created", "WaterSource", source.Id.ToString(), "Water source created.", cancellationToken);
        TempData["SuccessMessage"] = "Water source added.";
        return RedirectToAction(nameof(Sources));
    }

    [HttpGet]
    public async Task<IActionResult> EditSource(int id, CancellationToken cancellationToken)
    {
        var source = await db.WaterSources.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (source is null) return NotFound();
        var model = new WaterSourceFormViewModel
        {
            Id = source.Id,
            Name = source.Name,
            VillageId = source.VillageId,
            SourceType = source.SourceType,
            LocationDescription = source.LocationDescription,
            Latitude = source.Latitude,
            Longitude = source.Longitude,
            IsActive = source.IsActive
        };
        await LoadVillageLookupAsync(cancellationToken, model.VillageId);
        return View("SourceForm", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditSource(int id, WaterSourceFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id) return BadRequest();
        if (!Enum.IsDefined(model.SourceType))
            ModelState.AddModelError(nameof(model.SourceType), "Select a valid water source type.");
        if (await db.WaterSources.AnyAsync(x => x.Id != id && x.Name == model.Name.Trim() && x.VillageId == model.VillageId, cancellationToken))
            ModelState.AddModelError(nameof(model.Name), "A water source with this name already exists in the selected village.");
        if (!await db.Villages.AnyAsync(x => x.Id == model.VillageId, cancellationToken))
            ModelState.AddModelError(nameof(model.VillageId), "Select a valid village.");
        if (!ModelState.IsValid)
        {
            await LoadVillageLookupAsync(cancellationToken, model.VillageId);
            return View("SourceForm", model);
        }

        var source = await db.WaterSources.FindAsync([id], cancellationToken);
        if (source is null) return NotFound();
        ApplySource(source, model);
        await db.SaveChangesAsync(cancellationToken);
        await audit.RecordAsync("Updated", "WaterSource", id.ToString(), "Water source updated.", cancellationToken);
        TempData["SuccessMessage"] = "Water source updated.";
        return RedirectToAction(nameof(Sources));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSource(int id, CancellationToken cancellationToken)
    {
        var source = await db.WaterSources.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (source is null) return NotFound();
        if (await db.WaterQualityReports.AnyAsync(x => x.WaterSourceId == id, cancellationToken))
        {
            TempData["ErrorMessage"] = "This source has test history. Deactivate it instead of deleting it.";
            return RedirectToAction(nameof(Sources));
        }
        db.WaterSources.Remove(source);
        await db.SaveChangesAsync(cancellationToken);
        await audit.RecordAsync("Deleted", "WaterSource", id.ToString(), "Water source deleted.", cancellationToken);
        TempData["SuccessMessage"] = "Water source removed.";
        return RedirectToAction(nameof(Sources));
    }

    private async Task LoadReportLookupsAsync(CancellationToken cancellationToken, WaterQualityReportFormViewModel? model = null)
    {
        var sources = await db.WaterSources.AsNoTracking().Include(x => x.Village)
            .Where(x => x.IsActive || (model != null && x.Id == model.WaterSourceId))
            .OrderBy(x => x.Village.Name).ThenBy(x => x.Name).ToListAsync(cancellationToken);
        ViewBag.Sources = new SelectList(sources.Select(x => new SelectListItem($"{x.Name} — {x.Village.Name}", x.Id.ToString())),
            "Value", "Text", model?.WaterSourceId);
        ViewBag.HealthWorkers = new SelectList(await db.HealthWorkers.AsNoTracking()
            .Where(x => x.IsActive || (model != null && x.Id == model.TestedByHealthWorkerId))
            .OrderBy(x => x.FullName).ToListAsync(cancellationToken),
            "Id", "FullName", model?.TestedByHealthWorkerId);
    }

    private async Task LoadVillageLookupAsync(CancellationToken cancellationToken, int? selected = null) =>
        ViewBag.Villages = new SelectList(await db.Villages.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken), "Id", "Name", selected);

    private static WaterQualityReport ToReport(WaterQualityReportFormViewModel model)
    {
        var report = new WaterQualityReport();
        ApplyReport(report, model);
        return report;
    }

    private static void ApplyReport(WaterQualityReport report, WaterQualityReportFormViewModel model)
    {
        report.WaterSourceId = model.WaterSourceId;
        report.PhLevel = model.PhLevel;
        report.TurbidityNtu = model.TurbidityNtu;
        report.EColiResult = model.EColiResult;
        report.TotalColiformResult = model.TotalColiformResult;
        report.IsEstimated = model.IsEstimated;
        report.TestingDate = model.TestingDate.Date;
        report.TestedByHealthWorkerId = model.TestedByHealthWorkerId;
        report.Remarks = model.Remarks?.Trim();
        report.Status = model.Status;
    }

    private static WaterSource ToSource(WaterSourceFormViewModel model)
    {
        var source = new WaterSource();
        ApplySource(source, model);
        return source;
    }

    private static void ApplySource(WaterSource source, WaterSourceFormViewModel model)
    {
        source.Name = model.Name.Trim();
        source.VillageId = model.VillageId;
        source.SourceType = model.SourceType;
        source.LocationDescription = model.LocationDescription?.Trim();
        source.Latitude = model.Latitude;
        source.Longitude = model.Longitude;
        source.IsActive = model.IsActive;
    }
}
