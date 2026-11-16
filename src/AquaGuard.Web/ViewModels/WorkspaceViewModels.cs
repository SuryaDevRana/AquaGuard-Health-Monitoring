using System.ComponentModel.DataAnnotations;
using AquaGuard.Application.Common;
using AquaGuard.Application.EarlyWarnings;
using AquaGuard.Application.Patients;
using AquaGuard.Domain.Entities;
using AquaGuard.Domain.Enums;

namespace AquaGuard.Web.ViewModels;

public sealed record PatientIndexViewModel(
    PagedResult<PatientListItem> Results,
    IReadOnlyList<LookupItem> Villages,
    IReadOnlyList<LookupItem> Diseases,
    string? Search,
    int? VillageId,
    int? DiseaseId);

public sealed class PatientFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(160)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Range(0, 120)]
    public int Age { get; set; }

    public Gender Gender { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Select a village.")]
    [Display(Name = "Village")]
    public int VillageId { get; set; }

    [Phone, StringLength(30)]
    [Display(Name = "Contact number")]
    public string? ContactNumber { get; set; }

    [StringLength(400)]
    public string? Address { get; set; }

    [StringLength(1000)]
    public string? Symptoms { get; set; }

    [Display(Name = "Reported disease")]
    public int? DiseaseId { get; set; }
}

public sealed record SurveillanceIndexViewModel(IReadOnlyList<PatientReport> Reports, int TotalCases, int RecentCases, int DiseaseCount);

public sealed class PatientReportFormViewModel
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Select a patient.")]
    public int PatientId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Select a disease.")]
    public int DiseaseId { get; set; }

    [Required, StringLength(1000)]
    public string Symptoms { get; set; } = string.Empty;

    [DataType(DataType.DateTime)]
    [Display(Name = "Reported on")]
    public DateTime ReportedOn { get; set; } = DateTime.Now;

    [StringLength(32)]
    public string? Severity { get; set; }

    [StringLength(1500)]
    public string? Notes { get; set; }
}

public sealed class DiseaseFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Display(Name = "Water-borne")]
    public bool IsWaterBorne { get; set; } = true;
}

public sealed record WaterQualityIndexViewModel(
    IReadOnlyList<WaterQualityReport> Reports,
    int SafeCount,
    int AttentionCount,
    int PendingCount,
    int UnsafeCount);

public sealed class WaterQualityReportFormViewModel
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Select a water source.")]
    [Display(Name = "Water source")]
    public int WaterSourceId { get; set; }

    [Range(0, 14)]
    [Display(Name = "pH level")]
    public decimal? PhLevel { get; set; }

    [Range(0, 100000)]
    [Display(Name = "Turbidity (NTU)")]
    public decimal? TurbidityNtu { get; set; }

    [Display(Name = "E. coli result")]
    public TestResult EColiResult { get; set; }

    [Display(Name = "Total coliform result")]
    public TestResult TotalColiformResult { get; set; }

    [Display(Name = "Estimated values")]
    public bool IsEstimated { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Testing date")]
    public DateTime TestingDate { get; set; } = DateTime.Today;

    [Display(Name = "Tested by")]
    public int? TestedByHealthWorkerId { get; set; }

    [StringLength(1000)]
    public string? Remarks { get; set; }

    public QualityStatus Status { get; set; } = QualityStatus.Pending;
}

public sealed record WaterSourcesViewModel(IReadOnlyList<WaterSource> Sources);

public sealed class WaterSourceFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(160)]
    public string Name { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Select a village.")]
    [Display(Name = "Village")]
    public int VillageId { get; set; }

    [Display(Name = "Source type")]
    public WaterSourceType SourceType { get; set; }

    [StringLength(500)]
    [Display(Name = "Location description")]
    public string? LocationDescription { get; set; }

    [Range(-90, 90)]
    public decimal? Latitude { get; set; }

    [Range(-180, 180)]
    public decimal? Longitude { get; set; }

    [Display(Name = "Active source")]
    public bool IsActive { get; set; } = true;
}

public sealed record VillageIndexViewModel(IReadOnlyList<Village> Villages, int DistrictCount, int FacilityCount);

public sealed class VillageFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Select a district.")]
    public int DistrictId { get; set; }

    [Range(0, int.MaxValue)]
    public int Population { get; set; }

    [Range(0, int.MaxValue)]
    [Display(Name = "Households")]
    public int HouseholdCount { get; set; }

    [StringLength(500)]
    [Display(Name = "Available health facilities")]
    public string? AvailableHealthFacilities { get; set; }

    [Display(Name = "Assigned health worker")]
    public int? AssignedHealthWorkerId { get; set; }

    [Range(-90, 90)]
    public decimal? Latitude { get; set; }

    [Range(-180, 180)]
    public decimal? Longitude { get; set; }
}

public sealed record DistrictsIndexViewModel(IReadOnlyList<District> Districts);

public sealed class DistrictFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string State { get; set; } = string.Empty;
}

public sealed record FacilitiesIndexViewModel(IReadOnlyList<HealthFacility> Facilities);

public sealed class FacilityFormViewModel
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Select a village.")]
    public int VillageId { get; set; }

    [Required, StringLength(160)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(80)]
    [Display(Name = "Facility type")]
    public string FacilityType { get; set; } = "Primary Health Centre";

    [Phone, StringLength(30)]
    [Display(Name = "Contact number")]
    public string? ContactNumber { get; set; }

    [Display(Name = "Operational")]
    public bool IsOperational { get; set; } = true;
}

public sealed record HealthWorkersIndexViewModel(IReadOnlyList<HealthWorker> Workers);

public sealed class HealthWorkerFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(160)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Phone, StringLength(30)]
    [Display(Name = "Contact number")]
    public string? ContactNumber { get; set; }

    [StringLength(100)]
    public string? Designation { get; set; }

    [StringLength(80)]
    [Display(Name = "License number")]
    public string? LicenseNumber { get; set; }

    [Display(Name = "Active worker")]
    public bool IsActive { get; set; } = true;
}

public sealed record WarningsIndexViewModel(
    IReadOnlyList<EarlyWarning> Warnings,
    int TotalCount,
    int ActiveCount,
    int OpenCount,
    int AcknowledgedCount,
    int CriticalCount,
    int MatchingOpenCount,
    int MatchingAcknowledgedCount,
    string? Search,
    WarningStatus? Status,
    RiskLevel? RiskLevel,
    int? VillageId,
    IReadOnlyList<LookupItem> Villages);

public sealed class ManualWarningFormViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Select a village.")]
    [Display(Name = "Village")]
    public int VillageId { get; set; }

    [Required, EnumDataType(typeof(RiskLevel))]
    [Display(Name = "Risk level")]
    public RiskLevel? RiskLevel { get; set; }

    [Display(Name = "Related disease")]
    public int? RelatedDiseaseId { get; set; }

    [Required, StringLength(1000)]
    [Display(Name = "Warning message")]
    public string TriggerReason { get; set; } = string.Empty;

    [Range(0, 100000)]
    [Display(Name = "Related cases (if known)")]
    public int ReportedCaseCount { get; set; }

    [Required, StringLength(1500)]
    [Display(Name = "Recommended follow-up")]
    public string RecommendedAction { get; set; } =
        "Validate the available information and coordinate appropriate local follow-up. This warning is not a diagnosis or confirmed outbreak.";

    [StringLength(1500)]
    [Display(Name = "Evidence note (optional)")]
    public string? EvidenceSummary { get; set; }
}

public sealed record ReportsIndexViewModel(
    int PatientCount,
    int CaseCount,
    int VillageCount,
    int WaterTestCount,
    int ActiveWarningCount,
    IReadOnlyList<AuditLog> Activity);

public sealed record NotificationsIndexViewModel(IReadOnlyList<Notification> Notifications, int UnreadCount);

public sealed record SettingsPageViewModel(string FullName, string Email, EarlyWarningSettings WarningSettings, bool EmailConfigured);

public sealed record SearchResultItem(string Kind, string Name, string Detail, string Url);
public sealed record SearchResultsViewModel(string Query, IReadOnlyList<SearchResultItem> Results);
