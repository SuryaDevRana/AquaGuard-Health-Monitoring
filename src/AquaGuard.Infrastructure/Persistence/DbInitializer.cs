using AquaGuard.Domain.Entities;
using AquaGuard.Domain.Enums;
using AquaGuard.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AquaGuard.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task SeedDemoDataAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await db.Database.MigrateAsync(cancellationToken);

        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        const string adminEmail = "admin@aquaguard.demo";
        var admin = await userManager.FindByEmailAsync(adminEmail);
        var demoAdminPassword = Environment.GetEnvironmentVariable("AQUAGUARD_DEMO_ADMIN_PASSWORD");
        if (admin is null && !string.IsNullOrWhiteSpace(demoAdminPassword))
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = "AquaGuard Administrator"
            };
            var result = await userManager.CreateAsync(admin, demoAdminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, Roles.Administrator);
            }
        }

        if (await db.Villages.AnyAsync(cancellationToken))
        {
            return;
        }

        var districts = new[]
        {
            new District { Name = "Cachar", State = "Assam" },
            new District { Name = "West Garo Hills", State = "Meghalaya" },
            new District { Name = "Imphal East", State = "Manipur" },
            new District { Name = "Aizawl", State = "Mizoram" },
            new District { Name = "Kohima", State = "Nagaland" },
            new District { Name = "South Tripura", State = "Tripura" },
            new District { Name = "Lower Subansiri", State = "Arunachal Pradesh" },
            new District { Name = "Gyalshing", State = "Sikkim" }
        };
        db.Districts.AddRange(districts);

        var workers = new[]
        {
            new HealthWorker { FullName = "Dr. Ananya Das", ContactNumber = "+91 70020 10001", Designation = "Community Health Officer", LicenseNumber = "DEMO-CHW-101" },
            new HealthWorker { FullName = "R. Marak", ContactNumber = "+91 70020 10002", Designation = "Public Health Nurse", LicenseNumber = "DEMO-CHW-102" },
            new HealthWorker { FullName = "L. Chanu", ContactNumber = "+91 70020 10003", Designation = "Field Health Worker", LicenseNumber = "DEMO-CHW-103" },
            new HealthWorker { FullName = "K. Lalthanmawia", ContactNumber = "+91 70020 10004", Designation = "Water Quality Technician", LicenseNumber = "DEMO-CHW-104" },
            new HealthWorker { FullName = "T. Kikon", ContactNumber = "+91 70020 10005", Designation = "Community Health Officer", LicenseNumber = "DEMO-CHW-105" }
        };
        db.HealthWorkers.AddRange(workers);
        await db.SaveChangesAsync(cancellationToken);

        var villageSpecs = new[]
        {
            ("Borkhola", 0, 4310, 902, 24.92m, 92.98m),
            ("Rongram", 1, 2860, 584, 25.51m, 90.52m),
            ("Andro", 2, 3780, 765, 24.77m, 94.03m),
            ("Reiek", 3, 2210, 439, 23.84m, 92.59m),
            ("Khonoma", 4, 1940, 382, 25.68m, 94.03m),
            ("Jolaibari", 5, 3240, 641, 23.14m, 91.52m),
            ("Ziro", 6, 4120, 812, 27.55m, 93.82m),
            ("Rinchenpong", 7, 1730, 344, 27.31m, 88.25m)
        };
        var villages = villageSpecs.Select((v, index) => new Village
        {
            Name = v.Item1,
            DistrictId = districts[v.Item2].Id,
            Population = v.Item3,
            HouseholdCount = v.Item4,
            AvailableHealthFacilities = "Demonstration primary health services",
            AssignedHealthWorkerId = workers[index % workers.Length].Id,
            Latitude = v.Item5,
            Longitude = v.Item6
        }).ToArray();
        db.Villages.AddRange(villages);

        var diseases = new[]
        {
            new Disease { Name = "Cholera", Description = "Acute diarrhoeal illness monitored for demonstration." },
            new Disease { Name = "Typhoid", Description = "Water- and food-borne febrile illness monitored for demonstration." },
            new Disease { Name = "Diarrhoea", Description = "Acute diarrhoeal symptoms monitored for demonstration." },
            new Disease { Name = "Dysentery", Description = "Gastrointestinal illness monitored for demonstration." },
            new Disease { Name = "Hepatitis A", Description = "Viral liver illness with potential water-borne transmission." },
            new Disease { Name = "Other water-related illness", Description = "Other symptoms requiring follow-up." }
        };
        db.Diseases.AddRange(diseases);
        db.HealthFacilities.AddRange(villages.Select(v => new HealthFacility { Village = v, Name = $"{v.Name} Health Point", FacilityType = "Primary Health Centre", ContactNumber = "+91 70020 11000" }));
        await db.SaveChangesAsync(cancellationToken);

        var names = new[] { "Asha Devi", "Bikash Das", "Chandni Marak", "Deepak Singh", "Elina Chanu", "Farhan Ali", "Gita Rani", "Hminga Lian", "Ibohal Meitei", "Jaya Das", "Kanchan Devi", "Lalhruaia", "Maya Marak", "Nikhil Singh", "Oinam Rani", "Pema Bhutia", "Ratan Das", "Sangita Chanu", "Tashi Lepcha", "Uday Marak", "Vikram Das", "Wansuk Kikon", "Yangkai Lian", "Zoramthanga", "Anita Rai", "Bimal Debbarma", "Chitra Thapa", "Dinesh Meitei", "Ester Marak", "Firoz Ali" };
        var symptomOptions = new[] { "Fever and abdominal discomfort", "Loose stools and dehydration", "Nausea, fever and fatigue", "Abdominal cramps and diarrhoea", "Jaundice and loss of appetite", "Vomiting and watery stools" };
        var patients = names.Select((name, index) => new Patient
        {
            PatientCode = $"AGP-{index + 1:0000}",
            FullName = name,
            Age = 18 + (index * 3 % 52),
            Gender = (Gender)(index % 3),
            VillageId = villages[index % villages.Length].Id,
            ContactNumber = $"+91 7002{index + 10000:00000}",
            Address = $"Demonstration household {index + 1}",
            Symptoms = symptomOptions[index % symptomOptions.Length],
            ReportedDiseaseId = diseases[index % diseases.Length].Id,
            RegistrationDate = DateTime.UtcNow.AddDays(-(index + 2))
        }).ToArray();
        db.Patients.AddRange(patients);
        await db.SaveChangesAsync(cancellationToken);

        var reportDate = DateTime.UtcNow.Date;
        db.PatientReports.AddRange(Enumerable.Range(0, 42).Select(index => new PatientReport
        {
            PatientId = patients[index % patients.Length].Id,
            VillageId = villages[index % villages.Length].Id,
            DiseaseId = diseases[index % diseases.Length].Id,
            Symptoms = symptomOptions[index % symptomOptions.Length],
            ReportedOn = reportDate.AddDays(-(index % 21)).AddHours(8 + index % 9),
            Severity = index % 9 == 0 ? "Requires follow-up" : "Routine follow-up",
            Notes = "Demonstration case record. This entry is not public-health surveillance data.",
            RecordedByUserId = admin?.Id
        }));

        var sources = new[]
        {
            new WaterSource { Name = "Borkhola Community Tube Well", VillageId = villages[0].Id, SourceType = WaterSourceType.TubeWell, LocationDescription = "Demonstration community source" },
            new WaterSource { Name = "Rongram Spring", VillageId = villages[1].Id, SourceType = WaterSourceType.Spring, LocationDescription = "Demonstration spring source" },
            new WaterSource { Name = "Andro Public Tap", VillageId = villages[2].Id, SourceType = WaterSourceType.CommunityTap, LocationDescription = "Demonstration piped supply" },
            new WaterSource { Name = "Reiek Stream Intake", VillageId = villages[3].Id, SourceType = WaterSourceType.River, LocationDescription = "Demonstration surface-water intake" },
            new WaterSource { Name = "Khonoma Reservoir", VillageId = villages[4].Id, SourceType = WaterSourceType.CommunityTap, LocationDescription = "Demonstration reservoir outlet" }
        };
        db.WaterSources.AddRange(sources);
        await db.SaveChangesAsync(cancellationToken);

        db.WaterQualityReports.AddRange(Enumerable.Range(0, 20).Select(index =>
        {
            var isUnsafe = index is 1 or 6 or 11 or 16;
            return new WaterQualityReport
            {
                WaterSourceId = sources[index % sources.Length].Id,
                PhLevel = isUnsafe ? 6.2m : 6.8m + (index % 5) * .25m,
                TurbidityNtu = isUnsafe ? 8.4m : 1.1m + (index % 4) * .6m,
                EColiResult = isUnsafe ? TestResult.Unsafe : TestResult.Safe,
                TotalColiformResult = index % 7 == 0 ? TestResult.NotTested : (isUnsafe ? TestResult.Unsafe : TestResult.Safe),
                IsEstimated = index % 7 == 0,
                TestingDate = reportDate.AddDays(-(index * 2)),
                TestedByHealthWorkerId = workers[index % workers.Length].Id,
                Remarks = index % 7 == 0 ? "Demonstration estimate; confirmatory test pending." : "Demonstration laboratory observation.",
                Status = isUnsafe ? QualityStatus.Unsafe : (index % 7 == 0 ? QualityStatus.Pending : QualityStatus.Safe)
            };
        }));

        db.EarlyWarnings.AddRange(
            new EarlyWarning { AlertCode = "AGW-2026-001", VillageId = villages[0].Id, RiskLevel = RiskLevel.High, TriggerReason = "Demonstration: increased diarrhoeal case reports and an unsafe water-quality result within the review window.", RelatedDiseaseId = diseases[2].Id, ReportedCaseCount = 6, AlertDate = reportDate.AddDays(-1), RecommendedAction = "Arrange confirmatory water testing, conduct a local health follow-up, and share safe-water guidance.", Status = WarningStatus.Open, EvidenceSummary = "Rule-based demonstration alert; this is not a confirmed outbreak." },
            new EarlyWarning { AlertCode = "AGW-2026-002", VillageId = villages[3].Id, RiskLevel = RiskLevel.Moderate, TriggerReason = "Demonstration: repeated contamination observations at a monitored source.", ReportedCaseCount = 0, AlertDate = reportDate.AddDays(-3), RecommendedAction = "Repeat the water test and inspect the source protection measures.", Status = WarningStatus.Acknowledged, EvidenceSummary = "Rule-based demonstration alert; this is not a confirmed outbreak." },
            new EarlyWarning { AlertCode = "AGW-2026-003", VillageId = villages[1].Id, RiskLevel = RiskLevel.Low, TriggerReason = "Demonstration: symptom pattern meets observation threshold.", RelatedDiseaseId = diseases[1].Id, ReportedCaseCount = 3, AlertDate = reportDate.AddDays(-5), RecommendedAction = "Continue routine monitoring and validate new reports.", Status = WarningStatus.Resolved, EvidenceSummary = "Rule-based demonstration alert; this is not a confirmed outbreak.", ResolvedAt = reportDate.AddDays(-2) });

        db.Notifications.AddRange(
            new Notification { UserId = admin?.Id, Type = NotificationType.EarlyWarning, Title = "High-risk demonstration alert", Message = "Borkhola requires review of a rule-based demonstration alert.", Link = "/EarlyWarnings", IsRead = false },
            new Notification { UserId = admin?.Id, Type = NotificationType.WaterQuality, Title = "Water-quality observation recorded", Message = "A demonstration report has been marked unsafe and needs follow-up.", Link = "/WaterQuality", IsRead = false },
            new Notification { UserId = admin?.Id, Type = NotificationType.System, Title = "Demo data available", Message = "AquaGuard is showing simulated demonstration data, not real public-health surveillance.", Link = "/Dashboard", IsRead = true, ReadAt = reportDate });

        await db.SaveChangesAsync(cancellationToken);
    }
}
