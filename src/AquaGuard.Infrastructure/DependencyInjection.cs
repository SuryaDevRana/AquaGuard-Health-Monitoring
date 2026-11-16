using AquaGuard.Application.EarlyWarnings;
using AquaGuard.Application.Dashboard;
using AquaGuard.Application.Patients;
using AquaGuard.Application.Auditing;
using AquaGuard.Application.Email;
using AquaGuard.Infrastructure.Email;
using AquaGuard.Infrastructure.Identity;
using AquaGuard.Infrastructure.Persistence;
using AquaGuard.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AquaGuard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
        services.AddHttpContextAccessor();
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequiredLength = 10;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedAccount = false;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        services.Configure<EarlyWarningSettings>(configuration.GetSection(EarlyWarningSettings.SectionName));
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IEarlyWarningService, EarlyWarningService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IAuditService, AuditService>();
        return services;
    }
}
