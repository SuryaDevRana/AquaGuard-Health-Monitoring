using AquaGuard.Infrastructure.Persistence;
using AquaGuard.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AquaGuard.Web.Controllers;

[Authorize]
public sealed class SearchController(ApplicationDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? q, CancellationToken cancellationToken)
    {
        var term = q?.Trim();
        if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            return View(new SearchResultsViewModel(term ?? string.Empty, []));
        if (term.Length > 100) term = term[..100];

        var results = new List<SearchResultItem>();
        var patients = await db.Patients.AsNoTracking().Include(x => x.Village)
            .Where(x => x.FullName.Contains(term) || x.PatientCode.Contains(term))
            .OrderBy(x => x.FullName).Take(10).ToListAsync(cancellationToken);
        results.AddRange(patients.Select(x => new SearchResultItem(
            "Patient", x.FullName, $"{x.PatientCode} · {x.Village.Name}", Url.Action("Details", "Patients", new { id = x.Id })!)));

        var villages = await db.Villages.AsNoTracking().Include(x => x.District)
            .Where(x => x.Name.Contains(term) || x.District.Name.Contains(term))
            .OrderBy(x => x.Name).Take(10).ToListAsync(cancellationToken);
        results.AddRange(villages.Select(x => new SearchResultItem(
            "Village", x.Name, $"{x.District.Name}, {x.District.State}", Url.Action("Index", "Villages")!)));

        var sources = await db.WaterSources.AsNoTracking().Include(x => x.Village)
            .Where(x => x.Name.Contains(term))
            .OrderBy(x => x.Name).Take(10).ToListAsync(cancellationToken);
        results.AddRange(sources.Select(x => new SearchResultItem(
            "Water source", x.Name, x.Village.Name, Url.Action("Sources", "WaterQuality")!)));

        var diseases = await db.Diseases.AsNoTracking()
            .Where(x => x.Name.Contains(term))
            .OrderBy(x => x.Name).Take(10).ToListAsync(cancellationToken);
        results.AddRange(diseases.Select(x => new SearchResultItem(
            "Disease", x.Name, x.IsWaterBorne ? "Water-borne catalogue" : "Disease catalogue", Url.Action("Diseases", "Surveillance")!)));

        return View(new SearchResultsViewModel(term, results));
    }
}
