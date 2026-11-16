using AquaGuard.Domain.Common;
using AquaGuard.Domain.Entities;
using AquaGuard.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AquaGuard.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole, string>(options)
{
    public DbSet<District> Districts => Set<District>();
    public DbSet<Village> Villages => Set<Village>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Disease> Diseases => Set<Disease>();
    public DbSet<PatientReport> PatientReports => Set<PatientReport>();
    public DbSet<WaterSource> WaterSources => Set<WaterSource>();
    public DbSet<WaterQualityReport> WaterQualityReports => Set<WaterQualityReport>();
    public DbSet<HealthWorker> HealthWorkers => Set<HealthWorker>();
    public DbSet<EarlyWarning> EarlyWarnings => Set<EarlyWarning>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<HealthFacility> HealthFacilities => Set<HealthFacility>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                builder.Entity(entityType.ClrType).Property(nameof(BaseEntity.CreatedAt)).HasDefaultValueSql("SYSUTCDATETIME()");
                builder.Entity(entityType.ClrType).Property(nameof(BaseEntity.UpdatedAt)).HasDefaultValueSql("SYSUTCDATETIME()");
            }
        }

        builder.Entity<District>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.State).HasMaxLength(80).IsRequired();
            entity.HasIndex(x => new { x.Name, x.State }).IsUnique();
        });

        builder.Entity<Village>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.AvailableHealthFacilities).HasMaxLength(500);
            entity.Property(x => x.Latitude).HasPrecision(9, 6);
            entity.Property(x => x.Longitude).HasPrecision(9, 6);
            entity.HasIndex(x => new { x.Name, x.DistrictId }).IsUnique();
            entity.HasOne(x => x.AssignedHealthWorker).WithMany(x => x.AssignedVillages)
                .HasForeignKey(x => x.AssignedHealthWorkerId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Patient>(entity =>
        {
            entity.Property(x => x.PatientCode).HasMaxLength(32).IsRequired();
            entity.Property(x => x.FullName).HasMaxLength(160).IsRequired();
            entity.Property(x => x.ContactNumber).HasMaxLength(30);
            entity.Property(x => x.Address).HasMaxLength(400);
            entity.Property(x => x.Symptoms).HasMaxLength(1000);
            entity.Property(x => x.Gender).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(x => x.PatientCode).IsUnique();
            entity.HasIndex(x => new { x.VillageId, x.ReportedDiseaseId });
        });

        builder.Entity<Disease>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.HasIndex(x => x.Name).IsUnique();
        });

        builder.Entity<PatientReport>(entity =>
        {
            entity.Property(x => x.Symptoms).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.Severity).HasMaxLength(32);
            entity.Property(x => x.Notes).HasMaxLength(1500);
            entity.Property(x => x.RecordedByUserId).HasMaxLength(450);
            entity.HasIndex(x => new { x.VillageId, x.DiseaseId, x.ReportedOn });
            entity.HasOne(x => x.Patient).WithMany(x => x.Reports).HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<WaterSource>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.SourceType).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.LocationDescription).HasMaxLength(500);
            entity.Property(x => x.Latitude).HasPrecision(9, 6);
            entity.Property(x => x.Longitude).HasPrecision(9, 6);
            entity.HasIndex(x => new { x.Name, x.VillageId }).IsUnique();
        });

        builder.Entity<WaterQualityReport>(entity =>
        {
            entity.Property(x => x.PhLevel).HasPrecision(4, 2);
            entity.Property(x => x.TurbidityNtu).HasPrecision(7, 2);
            entity.Property(x => x.EColiResult).HasConversion<string>().HasMaxLength(16);
            entity.Property(x => x.TotalColiformResult).HasConversion<string>().HasMaxLength(16);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.Remarks).HasMaxLength(1000);
            entity.HasIndex(x => new { x.WaterSourceId, x.TestingDate });
            entity.HasOne(x => x.TestedByHealthWorker).WithMany(x => x.WaterQualityReports)
                .HasForeignKey(x => x.TestedByHealthWorkerId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<HealthWorker>(entity =>
        {
            entity.Property(x => x.FullName).HasMaxLength(160).IsRequired();
            entity.Property(x => x.ContactNumber).HasMaxLength(30);
            entity.Property(x => x.Designation).HasMaxLength(100);
            entity.Property(x => x.LicenseNumber).HasMaxLength(80);
            entity.HasIndex(x => x.UserId).IsUnique().HasFilter("[UserId] IS NOT NULL");
        });

        builder.Entity<EarlyWarning>(entity =>
        {
            entity.Property(x => x.AlertCode).HasMaxLength(32).IsRequired();
            entity.Property(x => x.RiskLevel).HasConversion<string>().HasMaxLength(16);
            entity.Property(x => x.TriggerReason).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.RecommendedAction).HasMaxLength(1500).IsRequired();
            entity.Property(x => x.EvidenceSummary).HasMaxLength(2000);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.AcknowledgedByUserId).HasMaxLength(450);
            entity.HasIndex(x => x.AlertCode).IsUnique();
            entity.HasIndex(x => new { x.VillageId, x.Status, x.AlertDate });
        });

        builder.Entity<Notification>(entity =>
        {
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.Title).HasMaxLength(180).IsRequired();
            entity.Property(x => x.Message).HasMaxLength(1500).IsRequired();
            entity.Property(x => x.Link).HasMaxLength(500);
            entity.Property(x => x.UserId).HasMaxLength(450);
            entity.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt });
        });

        builder.Entity<HealthFacility>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.FacilityType).HasMaxLength(80).IsRequired();
            entity.Property(x => x.ContactNumber).HasMaxLength(30);
        });

        builder.Entity<AuditLog>(entity =>
        {
            entity.Property(x => x.UserId).HasMaxLength(450);
            entity.Property(x => x.Action).HasMaxLength(120).IsRequired();
            entity.Property(x => x.EntityName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.EntityId).HasMaxLength(100);
            entity.Property(x => x.Details).HasMaxLength(4000);
            entity.Property(x => x.IpAddress).HasMaxLength(64);
            entity.HasIndex(x => new { x.EntityName, x.EntityId, x.CreatedAt });
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetAuditTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        SetAuditTimestamps();
        return base.SaveChanges();
    }

    private void SetAuditTimestamps()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
