using AquaGuard.Domain.Common;

namespace AquaGuard.Domain.Entities;

public class HealthFacility : BaseEntity
{
    public int VillageId { get; set; }
    public Village Village { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string FacilityType { get; set; } = string.Empty;
    public string? ContactNumber { get; set; }
    public bool IsOperational { get; set; } = true;
}
