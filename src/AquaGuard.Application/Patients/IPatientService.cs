using AquaGuard.Application.Common;
using AquaGuard.Domain.Enums;

namespace AquaGuard.Application.Patients;

public interface IPatientService
{
    Task<PagedResult<PatientListItem>> SearchAsync(PatientQuery query, CancellationToken cancellationToken = default);
    Task<PatientDetail?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LookupItem>> GetVillagesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LookupItem>> GetDiseasesAsync(CancellationToken cancellationToken = default);
    Task<int> CreateAsync(SavePatientCommand command, string? userId, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, SavePatientCommand command, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public sealed record PatientQuery(string? Search, int? VillageId, int? DiseaseId, int Page = 1, int PageSize = 10);
public sealed record LookupItem(int Id, string Name);
public sealed record PatientListItem(int Id, string PatientCode, string FullName, int Age, Gender Gender, string Village, string? Disease, DateTime RegisteredOn);
public sealed record PatientHistoryItem(int Id, string Disease, string Symptoms, string Severity, DateTime ReportedOn, string? Notes);
public sealed record PatientDetail(int Id, string PatientCode, string FullName, int Age, Gender Gender, int VillageId, string Village, string? ContactNumber, string? Address, string? Symptoms, int? DiseaseId, string? Disease, DateTime RegisteredOn, IReadOnlyList<PatientHistoryItem> History);
public sealed record SavePatientCommand(string FullName, int Age, Gender Gender, int VillageId, string? ContactNumber, string? Address, string? Symptoms, int? DiseaseId);
