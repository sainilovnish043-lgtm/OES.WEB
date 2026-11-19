using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OES.SERVICES.Notifications;

namespace OES.WEB.Pages.Api;

[Authorize]
[IgnoreAntiforgeryToken]
public sealed class NotificationsModel(INotificationService notificationService) : PageModel
{
    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        var unreadCount = await notificationService.GetUnreadCountAsync(userId, cancellationToken);
        var notifications = await notificationService.GetUserNotificationsAsync(userId, limit: 10, cancellationToken);

        return new JsonResult(new
        {
            unreadCount,
            items = notifications.Select(n => new
            {
                n.Id,
                n.Title,
                n.Message,
                n.Type,
                n.IsRead,
                n.ActionUrl,
                timeAgo = GetTimeAgo(n.CreatedUtc)
            })
        });
    }

    public async Task<IActionResult> OnPostMarkReadAsync(int id, CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        var success = await notificationService.MarkAsReadAsync(id, userId, cancellationToken);
        var remaining = await notificationService.GetUnreadCountAsync(userId, cancellationToken);
        return new JsonResult(new { success, unreadCount = remaining });
    }

    public async Task<IActionResult> OnPostMarkAllReadAsync(CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        var affected = await notificationService.MarkAllAsReadAsync(userId, cancellationToken);
        return new JsonResult(new { success = true, markedCount = affected, unreadCount = 0 });
    }

    private static string GetTimeAgo(DateTime utc)
    {
        var span = DateTime.UtcNow - utc;
        if (span.TotalMinutes < 1) return "just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
        if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
        return utc.ToLocalTime().ToString("MMM dd");
    }
}
