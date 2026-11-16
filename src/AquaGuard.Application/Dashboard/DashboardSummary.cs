namespace AquaGuard.Application.Dashboard;

public sealed class DashboardSummary
{
    public int TotalVillages { get; init; }
    public int RegisteredPatients { get; init; }
    public int DiseaseCases { get; init; }
    public int ActiveAlerts { get; init; }
    public IReadOnlyList<TrendPoint> DiseaseTrend { get; init; } = [];
    public IReadOnlyList<BreakdownItem> DiseaseDistribution { get; init; } = [];
    public IReadOnlyList<TrendPoint> WaterQualityTrend { get; init; } = [];
    public IReadOnlyList<BreakdownItem> VillageRisks { get; init; } = [];
    public IReadOnlyList<RecentPatientReportItem> RecentPatientReports { get; init; } = [];
    public IReadOnlyList<RecentWaterReportItem> RecentWaterReports { get; init; } = [];
    public IReadOnlyList<WarningItem> ActiveWarnings { get; init; } = [];
    public IReadOnlyList<ActivityItem> Activities { get; init; } = [];
}

public sealed record TrendPoint(string Label, decimal Value, decimal? SecondaryValue = null);
public sealed record BreakdownItem(string Label, decimal Value, string? Status = null);
public sealed record RecentPatientReportItem(int Id, string PatientName, string Village, string Disease, DateTime ReportedOn, string Severity);
public sealed record RecentWaterReportItem(int Id, string SourceName, string Village, string Status, DateTime TestedOn, bool IsEstimated);
public sealed record WarningItem(int Id, string AlertCode, string Village, string RiskLevel, string TriggerReason, DateTime AlertDate);
public sealed record ActivityItem(string Title, string Detail, DateTime OccurredAt);
