using System.Text;
using System.Globalization;
using AquaGuard.Domain.Enums;
using AquaGuard.Infrastructure.Persistence;
using AquaGuard.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AquaGuard.Web.Controllers;

[Authorize]
public sealed class ReportsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var activity = await db.AuditLogs.AsNoTracking().OrderByDescending(x => x.CreatedAt).Take(20).ToListAsync(cancellationToken);
        var model = new ReportsIndexViewModel(
            await db.Patients.CountAsync(cancellationToken),
            await db.PatientReports.CountAsync(cancellationToken),
            await db.Villages.CountAsync(cancellationToken),
            await db.WaterQualityReports.CountAsync(cancellationToken),
            await db.EarlyWarnings.CountAsync(x => x.Status == WarningStatus.Open || x.Status == WarningStatus.Acknowledged, cancellationToken),
            activity);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Export(string? kind, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(kind)) return BadRequest();
        var exportKind = kind.ToLowerInvariant();
        var csv = new StringBuilder();
        switch (exportKind)
        {
            case "cases":
                csv.AppendLine("Reported On,Patient Code,Village,Disease,Severity,Symptoms");
                var cases = await db.PatientReports.AsNoTracking().Include(x => x.Patient).Include(x => x.Village).Include(x => x.Disease)
                    .OrderByDescending(x => x.ReportedOn).ToListAsync(cancellationToken);
                foreach (var item in cases)
                    csv.AppendLine(Row(item.ReportedOn.ToString("yyyy-MM-dd HH:mm"), item.Patient.PatientCode, item.Village.Name, item.Disease.Name, item.Severity, item.Symptoms));
                break;
            case "water":
                csv.AppendLine("Testing Date,Source,Village,pH,Turbidity NTU,E. coli,Total coliform,Status,Estimated");
                var water = await db.WaterQualityReports.AsNoTracking().Include(x => x.WaterSource).ThenInclude(x => x.Village)
                    .OrderByDescending(x => x.TestingDate).ToListAsync(cancellationToken);
                foreach (var item in water)
                    csv.AppendLine(Row(item.TestingDate.ToString("yyyy-MM-dd"), item.WaterSource.Name, item.WaterSource.Village.Name,
                        item.PhLevel?.ToString(CultureInfo.InvariantCulture), item.TurbidityNtu?.ToString(CultureInfo.InvariantCulture), item.EColiResult.ToString(),
                        item.TotalColiformResult.ToString(), item.Status.ToString(), item.IsEstimated ? "Yes" : "No"));
                break;
            case "warnings":
                csv.AppendLine("Alert Code,Date,Village,Risk,Status,Disease,Reported Cases,Trigger Reason");
                var warnings = await db.EarlyWarnings.AsNoTracking().Include(x => x.Village).Include(x => x.RelatedDisease)
                    .OrderByDescending(x => x.AlertDate).ToListAsync(cancellationToken);
                foreach (var item in warnings)
                    csv.AppendLine(Row(item.AlertCode, item.AlertDate.ToString("yyyy-MM-dd"), item.Village.Name, item.RiskLevel.ToString(),
                        item.Status.ToString(), item.RelatedDisease?.Name, item.ReportedCaseCount.ToString(), item.TriggerReason));
                break;
            default:
                return NotFound();
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"aquaguard-{exportKind}-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    private static string Row(params string?[] values) => string.Join(",", values.Select(Csv));

    private static string Csv(string? value)
    {
        value ??= string.Empty;
        var firstNonWhitespace = value.TrimStart();
        if (firstNonWhitespace.Length > 0 && firstNonWhitespace[0] is '=' or '+' or '-' or '@' or '\t' or '\r')
            value = "'" + value;
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
