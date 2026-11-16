using AquaGuard.Domain.Common;

namespace AquaGuard.Domain.Entities;

public class PatientReport : BaseEntity
{
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public int VillageId { get; set; }
    public Village Village { get; set; } = null!;
    public int DiseaseId { get; set; }
    public Disease Disease { get; set; } = null!;
    public string Symptoms { get; set; } = string.Empty;
    public DateTime ReportedOn { get; set; } = DateTime.UtcNow;
    public string? Severity { get; set; }
    public string? Notes { get; set; }
    public string? RecordedByUserId { get; set; }
}
