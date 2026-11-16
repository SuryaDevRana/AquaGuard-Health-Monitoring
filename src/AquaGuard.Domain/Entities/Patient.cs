using AquaGuard.Domain.Common;
using AquaGuard.Domain.Enums;

namespace AquaGuard.Domain.Entities;

public class Patient : BaseEntity
{
    public string PatientCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int Age { get; set; }
    public Gender Gender { get; set; }
    public int VillageId { get; set; }
    public Village Village { get; set; } = null!;
    public string? ContactNumber { get; set; }
    public string? Address { get; set; }
    public string? Symptoms { get; set; }
    public int? ReportedDiseaseId { get; set; }
    public Disease? ReportedDisease { get; set; }
    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
    public ICollection<PatientReport> Reports { get; set; } = new List<PatientReport>();
}
