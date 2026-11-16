using AquaGuard.Domain.Entities;
using AquaGuard.Application.Auditing;
using AquaGuard.Infrastructure.Persistence;
using AquaGuard.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AquaGuard.Web.Controllers;

[Authorize]
public sealed class VillagesController(ApplicationDbContext db, IAuditService audit) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var villages = await db.Villages.AsNoTracking().Include(x => x.District).Include(x => x.AssignedHealthWorker)
            .OrderBy(x => x.Name).ToListAsync(cancellationToken);
        return View(new VillageIndexViewModel(
            villages,
            await db.Districts.CountAsync(cancellationToken),
            await db.HealthFacilities.CountAsync(cancellationToken)));
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        await LoadVillageLookupsAsync(cancellationToken);
        return View("Form", new VillageFormViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VillageFormViewModel model, CancellationToken cancellationToken)
    {
        await ValidateVillageReferencesAsync(model, cancellationToken);
        if (await db.Villages.AnyAsync(x => x.Name == model.Name.Trim() && x.DistrictId == model.DistrictId, cancellationToken))
            ModelState.AddModelError(nameof(model.Name), "A village with this name already exists in the selected district.");
        if (!ModelState.IsValid)
        {
            await LoadVillageLookupsAsync(cancellationToken, model);
            return View("Form", model);
        }

        var village = ToVillage(model);
        db.Villages.Add(village);
        await db.SaveChangesAsync(cancellationToken);
        await audit.RecordAsync("Created", "Village", village.Id.ToString(), "Village record created.", cancellationToken);
        TempData["SuccessMessage"] = "Village added.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var village = await db.Villages.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (village is null) return NotFound();
        var model = new VillageFormViewModel
        {
            Id = village.Id,
            Name = village.Name,
            DistrictId = village.DistrictId,
            Population = village.Population,
            HouseholdCount = village.HouseholdCount,
            AvailableHealthFacilities = village.AvailableHealthFacilities,
            AssignedHealthWorkerId = village.AssignedHealthWorkerId,
            Latitude = village.Latitude,
            Longitude = village.Longitude
        };
        await LoadVillageLookupsAsync(cancellationToken, model);
        return View("Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, VillageFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id) return BadRequest();
        await ValidateVillageReferencesAsync(model, cancellationToken);
        if (await db.Villages.AnyAsync(x => x.Id != id && x.Name == model.Name.Trim() && x.DistrictId == model.DistrictId, cancellationToken))
            ModelState.AddModelError(nameof(model.Name), "A village with this name already exists in the selected district.");
        if (!ModelState.IsValid)
        {
            await LoadVillageLookupsAsync(cancellationToken, model);
            return View("Form", model);
        }

        var village = await db.Villages.FindAsync([id], cancellationToken);
        if (village is null) return NotFound();
        ApplyVillage(village, model);
        await db.SaveChangesAsync(cancellationToken);
        await audit.RecordAsync("Updated", "Village", id.ToString(), "Village record updated.", cancellationToken);
        TempData["SuccessMessage"] = "Village updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var village = await db.Villages.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (village is null) return NotFound();
        if (await db.Patients.AnyAsync(x => x.VillageId == id, cancellationToken)
            || await db.WaterSources.AnyAsync(x => x.VillageId == id, cancellationToken)
            || await db.PatientReports.AnyAsync(x => x.VillageId == id, cancellationToken)
            || await db.EarlyWarnings.AnyAsync(x => x.VillageId == id, cancellationToken)
            || await db.HealthFacilities.AnyAsync(x => x.VillageId == id, cancellationToken))
        {
            TempData["ErrorMessage"] = "This village has linked records and cannot be deleted.";
            return RedirectToAction(nameof(Index));
        }
        db.Villages.Remove(village);
        await db.SaveChangesAsync(cancellationToken);
        await audit.RecordAsync("Deleted", "Village", id.ToString(), "Village record removed.", cancellationToken);
        TempData["SuccessMessage"] = "Village removed.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Districts(CancellationToken cancellationToken)
    {
        var districts = await db.Districts.AsNoTracking().Include(x => x.Villages)
            .OrderBy(x => x.State).ThenBy(x => x.Name).ToListAsync(cancellationToken);
        return View(new DistrictsIndexViewModel(districts));
    }

    [HttpGet]
    public IActionResult CreateDistrict() => View("DistrictForm", new DistrictFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDistrict(DistrictFormViewModel model, CancellationToken cancellationToken)
    {
        if (await db.Districts.AnyAsync(x => x.Name == model.Name.Trim() && x.State == model.State.Trim(), cancellationToken))
            ModelState.AddModelError(nameof(model.Name), "This district already exists in that state.");
        if (!ModelState.IsValid) return View("DistrictForm", model);
        var district = new District { Name = model.Name.Trim(), State = model.State.Trim() };
        db.Districts.Add(district);
        await db.SaveChangesAsync(cancellationToken);
        await audit.RecordAsync("Created", "District", district.Id.ToString(), "District record created.", cancellationToken);
        TempData["SuccessMessage"] = "District added.";
        return RedirectToAction(nameof(Districts));
    }

    [HttpGet]
    public async Task<IActionResult> EditDistrict(int id, CancellationToken cancellationToken)
    {
        var district = await db.Districts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (district is null) return NotFound();
        return View("DistrictForm", new DistrictFormViewModel { Id = id, Name = district.Name, State = district.State });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditDistrict(int id, DistrictFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id) return BadRequest();
        if (await db.Districts.AnyAsync(x => x.Id != id && x.Name == model.Name.Trim() && x.State == model.State.Trim(), cancellationToken))
            ModelState.AddModelError(nameof(model.Name), "This district already exists in that state.");
        if (!ModelState.IsValid) return View("DistrictForm", model);
        var district = await db.Districts.FindAsync([id], cancellationToken);
        if (district is null) return NotFound();
        district.Name = model.Name.Trim();
        district.State = model.State.Trim();
        await db.SaveChangesAsync(cancellationToken);
        await audit.RecordAsync("Updated", "District", id.ToString(), "District record updated.", cancellationToken);
        TempData["SuccessMessage"] = "District updated.";
        return RedirectToAction(nameof(Districts));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDistrict(int id, CancellationToken cancellationToken)
    {
        var district = await db.Districts.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (district is null) return NotFound();
        if (await db.Villages.AnyAsync(x => x.DistrictId == id, cancellationToken))
        {
            TempData["ErrorMessage"] = "This district still contains villages and cannot be deleted.";
            return RedirectToAction(nameof(Districts));
        }
        db.Districts.Remove(district);
        await db.SaveChangesAsync(cancellationToken);
        await audit.RecordAsync("Deleted", "District", id.ToString(), "District record removed.", cancellationToken);
        TempData["SuccessMessage"] = "District removed.";
        return RedirectToAction(nameof(Districts));
    }

    public async Task<IActionResult> Facilities(CancellationToken cancellationToken)
    {
        var facilities = await db.HealthFacilities.AsNoTracking().Include(x => x.Village)
            .OrderBy(x => x.Village.Name).ThenBy(x => x.Name).ToListAsync(cancellationToken);
        return View(new FacilitiesIndexViewModel(facilities));
    }

    [HttpGet]
    public async Task<IActionResult> CreateFacility(CancellationToken cancellationToken)
    {
        await LoadVillageLookupAsync(cancellationToken);
        return View("FacilityForm", new FacilityFormViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFacility(FacilityFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await db.Villages.AnyAsync(x => x.Id == model.VillageId, cancellationToken))
            ModelState.AddModelError(nameof(model.VillageId), "Select a valid village.");
        if (!ModelState.IsValid)
        {
            await LoadVillageLookupAsync(cancellationToken, model.VillageId);
            return View("FacilityForm", model);
        }
        var facility = ToFacility(model);
        db.HealthFacilities.Add(facility);
        await db.SaveChangesAsync(cancellationToken);
        await audit.RecordAsync("Created", "HealthFacility", facility.Id.ToString(), "Health facility record created.", cancellationToken);
        TempData["SuccessMessage"] = "Health facility added.";
        return RedirectToAction(nameof(Facilities));
    }

    [HttpGet]
    public async Task<IActionResult> EditFacility(int id, CancellationToken cancellationToken)
    {
        var facility = await db.HealthFacilities.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (facility is null) return NotFound();
        var model = new FacilityFormViewModel
        {
            Id = id,
            VillageId = facility.VillageId,
            Name = facility.Name,
            FacilityType = facility.FacilityType,
            ContactNumber = facility.ContactNumber,
            IsOperational = facility.IsOperational
        };
        await LoadVillageLookupAsync(cancellationToken, model.VillageId);
        return View("FacilityForm", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditFacility(int id, FacilityFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id) return BadRequest();
        if (!await db.Villages.AnyAsync(x => x.Id == model.VillageId, cancellationToken))
            ModelState.AddModelError(nameof(model.VillageId), "Select a valid village.");
        if (!ModelState.IsValid)
        {
            await LoadVillageLookupAsync(cancellationToken, model.VillageId);
            return View("FacilityForm", model);
        }
        var facility = await db.HealthFacilities.FindAsync([id], cancellationToken);
        if (facility is null) return NotFound();
        ApplyFacility(facility, model);
        await db.SaveChangesAsync(cancellationToken);
        await audit.RecordAsync("Updated", "HealthFacility", id.ToString(), "Health facility record updated.", cancellationToken);
        TempData["SuccessMessage"] = "Health facility updated.";
        return RedirectToAction(nameof(Facilities));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFacility(int id, CancellationToken cancellationToken)
    {
        var facility = await db.HealthFacilities.FindAsync([id], cancellationToken);
        if (facility is null) return NotFound();
        db.HealthFacilities.Remove(facility);
        await db.SaveChangesAsync(cancellationToken);
        await audit.RecordAsync("Deleted", "HealthFacility", id.ToString(), "Health facility record removed.", cancellationToken);
        TempData["SuccessMessage"] = "Health facility removed.";
        return RedirectToAction(nameof(Facilities));
    }

    private async Task LoadVillageLookupsAsync(CancellationToken cancellationToken, VillageFormViewModel? model = null)
    {
        var districts = await db.Districts.AsNoTracking().OrderBy(x => x.State).ThenBy(x => x.Name).ToListAsync(cancellationToken);
        ViewBag.Districts = new SelectList(districts.Select(x => new SelectListItem($"{x.Name} — {x.State}", x.Id.ToString())),
            "Value", "Text", model?.DistrictId);
        ViewBag.HealthWorkers = new SelectList(await db.HealthWorkers.AsNoTracking()
            .Where(x => x.IsActive || (model != null && x.Id == model.AssignedHealthWorkerId))
            .OrderBy(x => x.FullName).ToListAsync(cancellationToken),
            "Id", "FullName", model?.AssignedHealthWorkerId);
    }

    private async Task LoadVillageLookupAsync(CancellationToken cancellationToken, int? selected = null) =>
        ViewBag.Villages = new SelectList(await db.Villages.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken), "Id", "Name", selected);

    private async Task ValidateVillageReferencesAsync(VillageFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await db.Districts.AnyAsync(x => x.Id == model.DistrictId, cancellationToken))
            ModelState.AddModelError(nameof(model.DistrictId), "Select a valid district.");
        if (model.AssignedHealthWorkerId.HasValue
            && !await db.HealthWorkers.AnyAsync(x => x.Id == model.AssignedHealthWorkerId, cancellationToken))
            ModelState.AddModelError(nameof(model.AssignedHealthWorkerId), "Select a valid health worker.");
    }

    private static Village ToVillage(VillageFormViewModel model)
    {
        var village = new Village();
        ApplyVillage(village, model);
        return village;
    }

    private static void ApplyVillage(Village village, VillageFormViewModel model)
    {
        village.Name = model.Name.Trim();
        village.DistrictId = model.DistrictId;
        village.Population = model.Population;
        village.HouseholdCount = model.HouseholdCount;
        village.AvailableHealthFacilities = model.AvailableHealthFacilities?.Trim();
        village.AssignedHealthWorkerId = model.AssignedHealthWorkerId;
        village.Latitude = model.Latitude;
        village.Longitude = model.Longitude;
    }

    private static HealthFacility ToFacility(FacilityFormViewModel model)
    {
        var facility = new HealthFacility();
        ApplyFacility(facility, model);
        return facility;
    }

    private static void ApplyFacility(HealthFacility facility, FacilityFormViewModel model)
    {
        facility.VillageId = model.VillageId;
        facility.Name = model.Name.Trim();
        facility.FacilityType = model.FacilityType.Trim();
        facility.ContactNumber = model.ContactNumber?.Trim();
        facility.IsOperational = model.IsOperational;
    }
}
