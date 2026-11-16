namespace AquaGuard.Application.EarlyWarnings;

public sealed class EarlyWarningSettings
{
    public const string SectionName = "EarlyWarning";
    public int ModerateCaseThreshold { get; set; } = 3;
    public int HighCaseThreshold { get; set; } = 5;
    public int CriticalCaseThreshold { get; set; } = 8;
    public int LookbackDays { get; set; } = 7;
    public decimal MinimumPh { get; set; } = 6.5m;
    public decimal MaximumPh { get; set; } = 8.5m;
    public decimal MaximumTurbidityNtu { get; set; } = 5m;
    public int RepeatedContaminationThreshold { get; set; } = 2;
}
