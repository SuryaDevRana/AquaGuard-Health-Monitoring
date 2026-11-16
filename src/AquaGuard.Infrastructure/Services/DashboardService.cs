using AquaGuard.Application.Dashboard;
using AquaGuard.Domain.Enums;
using AquaGuard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AquaGuard.Infrastructure.Services;

public sealed class DashboardService(ApplicationDbContext db) : IDashboardService
{
    public async Task<DashboardSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var trendStart = today.AddDays(-27);
        var waterStart = today.AddDays(-29);

        var reportCounts = await db.PatientReports.AsNoTracking()
            .Where(x => x.ReportedOn >= trendStart)
            .GroupBy(x => x.ReportedOn.Date)
            .Select(x => new { Date = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.Date, x => x.Count, cancellationToken);

        var diseaseDistribution = await db.PatientReports.AsNoTracking()
            .Where(x => x.ReportedOn >= trendStart)
            .GroupBy(x => x.Disease.Name)
            .Select(x => new { Name = x.Key, Count = x.Count() })
            .OrderByDescending(x => x.Count)
            .Take(6)
            .ToListAsync(cancellationToken);

        var waterValues = await db.WaterQualityReports.AsNoTracking()
            .Where(x => x.TestingDate >= waterStart)
            .GroupBy(x => x.TestingDate.Date)
            .Select(x => new { Date = x.Key, Ph = x.Average(y => y.PhLevel), Turbidity = x.Average(y => y.TurbidityNtu) })
            .ToListAsync(cancellationToken);
        var waterByDate = waterValues.ToDictionary(x => x.Date);

        var villageRiskGroups = await db.EarlyWarnings.AsNoTracking()
            .Where(x => x.Status == WarningStatus.Open || x.Status == WarningStatus.Acknowledged)
            .GroupBy(x => new { x.Village.Name, x.RiskLevel })
            .Select(x => new { x.Key.Name, x.Key.RiskLevel, Count = x.Count() })
            .ToListAsync(cancellationToken);
        var villageRisks = villageRiskGroups.OrderByDescending(x => x.RiskLevel)
            .ThenByDescending(x => x.Count).Take(6).ToList();

        var recentPatientReports = await db.PatientReports.AsNoTracking()
            .OrderByDescending(x => x.ReportedOn).Take(6)
            .Select(x => new RecentPatientReportItem(x.Id, x.Patient.FullName, x.Village.Name, x.Disease.Name, x.ReportedOn, x.Severity ?? "Recorded"))
            .ToListAsync(cancellationToken);

        var recentWaterReports = await db.WaterQualityReports.AsNoTracking()
            .OrderByDescending(x => x.TestingDate).Take(6)
            .Select(x => new RecentWaterReportItem(x.Id, x.WaterSource.Name, x.WaterSource.Village.Name, x.Status.ToString(), x.TestingDate, x.IsEstimated))
            .ToListAsync(cancellationToken);

        var warnings = await db.EarlyWarnings.AsNoTracking()
            .Where(x => x.Status == WarningStatus.Open || x.Status == WarningStatus.Acknowledged)
            .OrderByDescending(x => x.RiskLevel).ThenByDescending(x => x.AlertDate).Take(5)
            .Select(x => new WarningItem(x.Id, x.AlertCode, x.Village.Name, x.RiskLevel.ToString(), x.TriggerReason, x.AlertDate))
            .ToListAsync(cancellationToken);

        var activities = await db.AuditLogs.AsNoTracking().OrderByDescending(x => x.CreatedAt).Take(5)
            .Select(x => new ActivityItem(x.Action, x.Details ?? x.EntityName, x.CreatedAt)).ToListAsync(cancellationToken);
        if (activities.Count == 0)
        {
            activities = recentPatientReports.Take(3).Select(x => new ActivityItem("Patient report recorded", $"{x.Disease} report from {x.Village}", x.ReportedOn)).ToList();
        }

        return new DashboardSummary
        {
            TotalVillages = await db.Villages.CountAsync(cancellationToken),
            RegisteredPatients = await db.Patients.CountAsync(cancellationToken),
            DiseaseCases = await db.PatientReports.CountAsync(cancellationToken),
            ActiveAlerts = await db.EarlyWarnings.CountAsync(x => x.Status == WarningStatus.Open || x.Status == WarningStatus.Acknowledged, cancellationToken),
            DiseaseTrend = Enumerable.Range(0, 28).Select(offset =>
            {
                var date = trendStart.AddDays(offset);
                return new TrendPoint(date.ToString("dd MMM"), reportCounts.GetValueOrDefault(date));
            }).ToList(),
            DiseaseDistribution = diseaseDistribution.Select(x => new BreakdownItem(x.Name, x.Count)).ToList(),
            WaterQualityTrend = Enumerable.Range(0, 6).Select(offset =>
            {
                var date = today.AddDays(-25 + (offset * 5));
                var matching = waterByDate.Where(x => x.Key >= date && x.Key < date.AddDays(5)).Select(x => x.Value).ToList();
                var ph = matching.Count == 0 ? 0 : matching.Average(x => x.Ph ?? 0);
                var turbidity = matching.Count == 0 ? 0 : matching.Average(x => x.Turbidity ?? 0);
                return new TrendPoint(date.ToString("dd MMM"), ph, turbidity);
            }).ToList(),
            VillageRisks = villageRisks.Select(x => new BreakdownItem(x.Name, x.Count, x.RiskLevel.ToString())).ToList(),
            RecentPatientReports = recentPatientReports,
            RecentWaterReports = recentWaterReports,
            ActiveWarnings = warnings,
            Activities = activities
        };
    }
}
