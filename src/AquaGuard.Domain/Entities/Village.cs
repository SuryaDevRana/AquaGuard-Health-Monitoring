using AquaGuard.Domain.Common;

namespace AquaGuard.Domain.Entities;

public class Village : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int DistrictId { get; set; }
    public District District { get; set; } = null!;
    public int Population { get; set; }
    public int HouseholdCount { get; set; }
    public string? AvailableHealthFacilities { get; set; }
    public int? AssignedHealthWorkerId { get; set; }
    public HealthWorker? AssignedHealthWorker { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public ICollection<Patient> Patients { get; set; } = new List<Patient>();
    public ICollection<WaterSource> WaterSources { get; set; } = new List<WaterSource>();
    public ICollection<PatientReport> PatientReports { get; set; } = new List<PatientReport>();
    public ICollection<EarlyWarning> EarlyWarnings { get; set; } = new List<EarlyWarning>();
    public ICollection<HealthFacility> HealthFacilities { get; set; } = new List<HealthFacility>();
}
