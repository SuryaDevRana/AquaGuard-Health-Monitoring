using AquaGuard.Application.Common;
using AquaGuard.Application.Patients;
using AquaGuard.Domain.Entities;
using AquaGuard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AquaGuard.Infrastructure.Services;

public sealed class PatientService(ApplicationDbContext db) : IPatientService
{
    public async Task<PagedResult<PatientListItem>> SearchAsync(PatientQuery query, CancellationToken cancellationToken = default)
    {
        var source = db.Patients.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            source = source.Where(x => x.FullName.Contains(term) || x.PatientCode.Contains(term) || (x.ContactNumber != null && x.ContactNumber.Contains(term)));
        }
        if (query.VillageId.HasValue) source = source.Where(x => x.VillageId == query.VillageId);
        if (query.DiseaseId.HasValue) source = source.Where(x => x.ReportedDiseaseId == query.DiseaseId);

        var total = await source.CountAsync(cancellationToken);
        var page = Math.Max(1, query.Page);
        var items = await source.OrderByDescending(x => x.RegistrationDate).ThenBy(x => x.FullName)
            .Skip((page - 1) * query.PageSize).Take(query.PageSize)
            .Select(x => new PatientListItem(x.Id, x.PatientCode, x.FullName, x.Age, x.Gender, x.Village.Name, x.ReportedDisease == null ? null : x.ReportedDisease.Name, x.RegistrationDate))
            .ToListAsync(cancellationToken);
        return new PagedResult<PatientListItem>(items, total, page, query.PageSize);
    }

    public Task<IReadOnlyList<LookupItem>> GetVillagesAsync(CancellationToken cancellationToken = default) => db.Villages.AsNoTracking().OrderBy(x => x.Name).Select(x => new LookupItem(x.Id, x.Name)).ToListAsync(cancellationToken).ContinueWith(x => (IReadOnlyList<LookupItem>)x.Result, cancellationToken);
    public Task<IReadOnlyList<LookupItem>> GetDiseasesAsync(CancellationToken cancellationToken = default) => db.Diseases.AsNoTracking().OrderBy(x => x.Name).Select(x => new LookupItem(x.Id, x.Name)).ToListAsync(cancellationToken).ContinueWith(x => (IReadOnlyList<LookupItem>)x.Result, cancellationToken);

    public async Task<PatientDetail?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var patient = await db.Patients.AsNoTracking().Where(x => x.Id == id).Select(x => new
        {
            x.Id, x.PatientCode, x.FullName, x.Age, x.Gender, x.VillageId, Village = x.Village.Name, x.ContactNumber, x.Address, x.Symptoms, x.ReportedDiseaseId,
            Disease = x.ReportedDisease == null ? null : x.ReportedDisease.Name, x.RegistrationDate,
            History = x.Reports.OrderByDescending(r => r.ReportedOn).Select(r => new PatientHistoryItem(r.Id, r.Disease.Name, r.Symptoms, r.Severity ?? "Recorded", r.ReportedOn, r.Notes)).ToList()
        }).SingleOrDefaultAsync(cancellationToken);
        return patient is null ? null : new PatientDetail(patient.Id, patient.PatientCode, patient.FullName, patient.Age, patient.Gender, patient.VillageId, patient.Village, patient.ContactNumber, patient.Address, patient.Symptoms, patient.ReportedDiseaseId, patient.Disease, patient.RegistrationDate, patient.History);
    }

    public async Task<int> CreateAsync(SavePatientCommand command, string? userId, CancellationToken cancellationToken = default)
    {
        var patient = new Patient
        {
            PatientCode = $"AGP-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
            FullName = command.FullName.Trim(), Age = command.Age, Gender = command.Gender, VillageId = command.VillageId,
            ContactNumber = command.ContactNumber?.Trim(), Address = command.Address?.Trim(), Symptoms = command.Symptoms?.Trim(), ReportedDiseaseId = command.DiseaseId
        };
        db.Patients.Add(patient);
        await db.SaveChangesAsync(cancellationToken);

        if (command.DiseaseId.HasValue)
        {
            db.PatientReports.Add(new PatientReport { PatientId = patient.Id, VillageId = patient.VillageId, DiseaseId = command.DiseaseId.Value, Symptoms = command.Symptoms?.Trim() ?? "No symptoms recorded", Severity = "Routine follow-up", RecordedByUserId = userId, Notes = "Initial report created with patient registration." });
            await db.SaveChangesAsync(cancellationToken);
        }
        return patient.Id;
    }

    public async Task<bool> UpdateAsync(int id, SavePatientCommand command, CancellationToken cancellationToken = default)
    {
        var patient = await db.Patients.FindAsync([id], cancellationToken);
        if (patient is null) return false;
        patient.FullName = command.FullName.Trim(); patient.Age = command.Age; patient.Gender = command.Gender; patient.VillageId = command.VillageId;
        patient.ContactNumber = command.ContactNumber?.Trim(); patient.Address = command.Address?.Trim(); patient.Symptoms = command.Symptoms?.Trim(); patient.ReportedDiseaseId = command.DiseaseId;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var patient = await db.Patients.Include(x => x.Reports).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (patient is null) return false;
        db.PatientReports.RemoveRange(patient.Reports);
        db.Patients.Remove(patient);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
