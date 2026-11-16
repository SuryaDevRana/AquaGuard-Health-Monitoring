namespace AquaGuard.Application.Auditing;

public interface IAuditService
{
    Task RecordAsync(
        string action,
        string entityName,
        string? entityId = null,
        string? details = null,
        CancellationToken cancellationToken = default,
        string? actorUserId = null);
}
