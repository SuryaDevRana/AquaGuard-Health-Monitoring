using AquaGuard.Domain.Entities;
using AquaGuard.Application.Auditing;
using AquaGuard.Infrastructure.Persistence;
using AquaGuard.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AquaGuard.Web.Controllers;

[Authorize]
public sealed class HealthWorkersController(ApplicationDbContext db, IAuditService audit) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var workers = await db.HealthWorkers.AsNoTracking()
            .Include(x => x.AssignedVillages)
            .Include(x => x.WaterQualityReports)
            .OrderByDescending(x => x.IsActive).ThenBy(x => x.FullName)
            .ToListAsync(cancellationToken);
        return View(new HealthWorkersIndexViewModel(workers));
    }

    [HttpGet]
    public IActionResult Create() => View("Form", new HealthWorkerFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(HealthWorkerFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View("Form", model);
        var worker = ToWorker(model);
        db.HealthWorkers.Add(worker);
        await db.SaveChangesAsync(cancellationToken);
        await audit.RecordAsync("Created", "HealthWorker", worker.Id.ToString(), "Health worker profile created.", cancellationToken);
        TempData["SuccessMessage"] = "Health worker added.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var worker = await db.HealthWorkers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (worker is null) return NotFound();
        return View("Form", new HealthWorkerFormViewModel
        {
            Id = worker.Id,
            FullName = worker.FullName,
            ContactNumber = worker.ContactNumber,
            Designation = worker.Designation,
            LicenseNumber = worker.LicenseNumber,
            IsActive = worker.IsActive
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, HealthWorkerFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View("Form", model);
        var worker = await db.HealthWorkers.FindAsync([id], cancellationToken);
        if (worker is null) return NotFound();
        Apply(worker, model);
        await db.SaveChangesAsync(cancellationToken);
        await audit.RecordAsync("Updated", "HealthWorker", id.ToString(), "Health worker profile updated.", cancellationToken);
        TempData["SuccessMessage"] = "Health worker updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken)
    {
        var worker = await db.HealthWorkers.FindAsync([id], cancellationToken);
        if (worker is null) return NotFound();
        worker.IsActive = !worker.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        await audit.RecordAsync(worker.IsActive ? "Reactivated" : "Deactivated", "HealthWorker", id.ToString(), "Health worker active status changed.", cancellationToken);
        TempData["SuccessMessage"] = worker.IsActive ? "Health worker reactivated." : "Health worker deactivated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var worker = await db.HealthWorkers.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (worker is null) return NotFound();
        if (await db.Villages.AnyAsync(x => x.AssignedHealthWorkerId == id, cancellationToken)
            || await db.WaterQualityReports.AnyAsync(x => x.TestedByHealthWorkerId == id, cancellationToken))
        {
            TempData["ErrorMessage"] = "This worker has linked community records. Deactivate the profile instead of deleting it.";
            return RedirectToAction(nameof(Index));
        }
        db.HealthWorkers.Remove(worker);
        await db.SaveChangesAsync(cancellationToken);
        await audit.RecordAsync("Deleted", "HealthWorker", id.ToString(), "Health worker profile removed.", cancellationToken);
        TempData["SuccessMessage"] = "Health worker removed.";
        return RedirectToAction(nameof(Index));
    }

    private static HealthWorker ToWorker(HealthWorkerFormViewModel model)
    {
        var worker = new HealthWorker();
        Apply(worker, model);
        return worker;
    }

    private static void Apply(HealthWorker worker, HealthWorkerFormViewModel model)
    {
        worker.FullName = model.FullName.Trim();
        worker.ContactNumber = model.ContactNumber?.Trim();
        worker.Designation = model.Designation?.Trim();
        worker.LicenseNumber = model.LicenseNumber?.Trim();
        worker.IsActive = model.IsActive;
    }
}
