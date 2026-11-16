using AquaGuard.Domain.Common;
using AquaGuard.Domain.Enums;

namespace AquaGuard.Domain.Entities;

public class Notification : BaseEntity
{
    public string? UserId { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Link { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
}
