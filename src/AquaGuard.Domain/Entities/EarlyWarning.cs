using AquaGuard.Domain.Common;
using AquaGuard.Domain.Enums;

namespace AquaGuard.Domain.Entities;

public class EarlyWarning : BaseEntity
{
    public string AlertCode { get; set; } = string.Empty;
    public int VillageId { get; set; }
    public Village Village { get; set; } = null!;
    public RiskLevel RiskLevel { get; set; }
    public string TriggerReason { get; set; } = string.Empty;
    public int? RelatedDiseaseId { get; set; }
    public Disease? RelatedDisease { get; set; }
    public int ReportedCaseCount { get; set; }
    public DateTime AlertDate { get; set; } = DateTime.UtcNow;
    public string RecommendedAction { get; set; } = string.Empty;
    public WarningStatus Status { get; set; } = WarningStatus.Open;
    public string? EvidenceSummary { get; set; }
    public string? AcknowledgedByUserId { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
