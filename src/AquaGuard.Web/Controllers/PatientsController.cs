using System.Security.Claims;
using AquaGuard.Application.Auditing;
using AquaGuard.Application.Patients;
using AquaGuard.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AquaGuard.Web.Controllers;

[Authorize]
public sealed class PatientsController(IPatientService patients, IAuditService audit) : Controller
{
    public async Task<IActionResult> Index(string? search, int? villageId, int? diseaseId, int page = 1, CancellationToken cancellationToken = default)
    {
        var query = new PatientQuery(search, villageId, diseaseId, page, 10);
        var results = await patients.SearchAsync(query, cancellationToken);
        var model = new PatientIndexViewModel(
            results,
            await patients.GetVillagesAsync(cancellationToken),
            await patients.GetDiseasesAsync(cancellationToken),
            search, villageId, diseaseId);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        await LoadLookupsAsync(cancellationToken);
        return View("Form", new PatientFormViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PatientFormViewModel model, CancellationToken cancellationToken)
    {
        await ValidateReferencesAsync(model, cancellationToken);
        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync(cancellationToken, model);
            return View("Form", model);
        }

        var id = await patients.CreateAsync(ToCommand(model), User.FindFirstValue(ClaimTypes.NameIdentifier), cancellationToken);
        await audit.RecordAsync("Created", "Patient", id.ToString(), "Patient profile created.", cancellationToken);
        TempData["SuccessMessage"] = "Patient record created.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var patient = await patients.GetAsync(id, cancellationToken);
        return patient is null ? NotFound() : View(patient);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var patient = await patients.GetAsync(id, cancellationToken);
        if (patient is null) return NotFound();
        var model = new PatientFormViewModel
        {
            Id = patient.Id,
            FullName = patient.FullName,
            Age = patient.Age,
            Gender = patient.Gender,
            VillageId = patient.VillageId,
            ContactNumber = patient.ContactNumber,
            Address = patient.Address,
            Symptoms = patient.Symptoms,
            DiseaseId = patient.DiseaseId
        };
        await LoadLookupsAsync(cancellationToken, model);
        return View("Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PatientFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id) return BadRequest();
        await ValidateReferencesAsync(model, cancellationToken);
        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync(cancellationToken, model);
            return View("Form", model);
        }

        if (!await patients.UpdateAsync(id, ToCommand(model), cancellationToken)) return NotFound();
        await audit.RecordAsync("Updated", "Patient", id.ToString(), "Patient profile updated.", cancellationToken);
        TempData["SuccessMessage"] = "Patient record updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (!await patients.DeleteAsync(id, cancellationToken)) return NotFound();
        await audit.RecordAsync("Deleted", "Patient", id.ToString(), "Patient profile and associated case reports deleted.", cancellationToken);
        TempData["SuccessMessage"] = "Patient record deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadLookupsAsync(CancellationToken cancellationToken, PatientFormViewModel? model = null)
    {
        ViewBag.Villages = new SelectList(await patients.GetVillagesAsync(cancellationToken), "Id", "Name", model?.VillageId);
        ViewBag.Diseases = new SelectList(await patients.GetDiseasesAsync(cancellationToken), "Id", "Name", model?.DiseaseId);
    }

    private async Task ValidateReferencesAsync(PatientFormViewModel model, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(model.Gender))
            ModelState.AddModelError(nameof(model.Gender), "Select a valid gender option.");
        if (!(await patients.GetVillagesAsync(cancellationToken)).Any(x => x.Id == model.VillageId))
            ModelState.AddModelError(nameof(model.VillageId), "Select a valid village.");
        if (model.DiseaseId.HasValue && !(await patients.GetDiseasesAsync(cancellationToken)).Any(x => x.Id == model.DiseaseId))
            ModelState.AddModelError(nameof(model.DiseaseId), "Select a valid disease.");
    }

    private static SavePatientCommand ToCommand(PatientFormViewModel model) =>
        new(model.FullName, model.Age, model.Gender, model.VillageId, model.ContactNumber, model.Address, model.Symptoms, model.DiseaseId);
}
