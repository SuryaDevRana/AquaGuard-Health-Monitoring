using AquaGuard.Domain.Common;
using AquaGuard.Domain.Enums;

namespace AquaGuard.Domain.Entities;

public class WaterSource : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int VillageId { get; set; }
    public Village Village { get; set; } = null!;
    public WaterSourceType SourceType { get; set; }
    public string? LocationDescription { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<WaterQualityReport> QualityReports { get; set; } = new List<WaterQualityReport>();
}
