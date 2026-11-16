using System.Security.Claims;
using AquaGuard.Application.Auditing;
using AquaGuard.Infrastructure.Persistence;
using AquaGuard.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AquaGuard.Web.Controllers;

[Authorize]
public sealed class NotificationsController(ApplicationDbContext db, IAuditService audit) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var notifications = await db.Notifications.AsNoTracking()
            .Where(x => x.UserId == null || x.UserId == userId)
            .OrderBy(x => x.IsRead).ThenByDescending(x => x.CreatedAt)
            .Take(100).ToListAsync(cancellationToken);
        return View(new NotificationsIndexViewModel(notifications, notifications.Count(x => !x.IsRead)));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var notification = await db.Notifications.SingleOrDefaultAsync(
            x => x.Id == id && (x.UserId == null || x.UserId == userId), cancellationToken);
        if (notification is null) return NotFound();
        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await audit.RecordAsync("Read", "Notification", id.ToString(), "Notification marked as read.", cancellationToken);
        return RedirectToLocal(notification.Link);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var unread = await db.Notifications
            .Where(x => !x.IsRead && (x.UserId == null || x.UserId == userId))
            .ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        foreach (var notification in unread)
        {
            notification.IsRead = true;
            notification.ReadAt = now;
        }
        await db.SaveChangesAsync(cancellationToken);
        await audit.RecordAsync("Read all", "Notification", null, $"{unread.Count} notification(s) marked as read.", cancellationToken);
        TempData["SuccessMessage"] = $"{unread.Count} notification(s) marked as read.";
        return RedirectToAction(nameof(Index));
    }

    private IActionResult RedirectToLocal(string? link) =>
        !string.IsNullOrWhiteSpace(link) && Url.IsLocalUrl(link) ? Redirect(link) : RedirectToAction(nameof(Index));
}
