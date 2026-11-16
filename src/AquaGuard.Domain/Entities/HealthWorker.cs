using AquaGuard.Domain.Common;

namespace AquaGuard.Domain.Entities;

public class HealthWorker : BaseEntity
{
    public string? UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? ContactNumber { get; set; }
    public string? Designation { get; set; }
    public string? LicenseNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Village> AssignedVillages { get; set; } = new List<Village>();
    public ICollection<WaterQualityReport> WaterQualityReports { get; set; } = new List<WaterQualityReport>();
}
