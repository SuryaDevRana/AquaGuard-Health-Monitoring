using AquaGuard.Domain.Common;
using AquaGuard.Domain.Enums;

namespace AquaGuard.Domain.Entities;

public class WaterQualityReport : BaseEntity
{
    public int WaterSourceId { get; set; }
    public WaterSource WaterSource { get; set; } = null!;
    public decimal? PhLevel { get; set; }
    public decimal? TurbidityNtu { get; set; }
    public TestResult EColiResult { get; set; } = TestResult.NotTested;
    public TestResult TotalColiformResult { get; set; } = TestResult.NotTested;
    public bool IsEstimated { get; set; }
    public DateTime TestingDate { get; set; } = DateTime.UtcNow;
    public int? TestedByHealthWorkerId { get; set; }
    public HealthWorker? TestedByHealthWorker { get; set; }
    public string? Remarks { get; set; }
    public QualityStatus Status { get; set; } = QualityStatus.Pending;
}
