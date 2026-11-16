namespace AquaGuard.Infrastructure.Identity;

public static class Roles
{
    public const string Administrator = "Administrator";
    public const string HealthcareWorker = "Healthcare Worker";
    public static readonly string[] All = [Administrator, HealthcareWorker];
}
