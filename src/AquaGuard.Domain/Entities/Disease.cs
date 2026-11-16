using AquaGuard.Domain.Common;

namespace AquaGuard.Domain.Entities;

public class Disease : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsWaterBorne { get; set; } = true;
    public ICollection<Patient> Patients { get; set; } = new List<Patient>();
    public ICollection<PatientReport> PatientReports { get; set; } = new List<PatientReport>();
    public ICollection<EarlyWarning> EarlyWarnings { get; set; } = new List<EarlyWarning>();
}
