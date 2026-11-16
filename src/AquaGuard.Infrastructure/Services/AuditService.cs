using System.Security.Claims;
using AquaGuard.Application.Auditing;
using AquaGuard.Domain.Entities;
using AquaGuard.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;

namespace AquaGuard.Infrastructure.Services;

public sealed class AuditService(ApplicationDbContext db, IHttpContextAccessor httpContextAccessor) : IAuditService
{
    public async Task RecordAsync(
        string action,
        string entityName,
        string? entityId = null,
        string? details = null,
        CancellationToken cancellationToken = default,
        string? actorUserId = null)
    {
        var context = httpContextAccessor.HttpContext;
        db.AuditLogs.Add(new AuditLog
        {
            UserId = actorUserId ?? context?.User.FindFirstValue(ClaimTypes.NameIdentifier),
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Details = details,
            IpAddress = context?.Connection.RemoteIpAddress?.ToString()
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}
