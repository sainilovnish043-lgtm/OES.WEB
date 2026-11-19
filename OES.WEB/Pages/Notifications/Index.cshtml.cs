using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OES.MODELS.Notifications;
using OES.SERVICES.Notifications;

namespace OES.WEB.Pages.Notifications;

[Authorize]
public sealed class IndexModel(INotificationService notificationService) : PageModel
{
    public IReadOnlyList<Notification> Notifications { get; private set; } = [];
    public int UnreadCount { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId))
        {
            return Forbid();
        }

        UnreadCount = await notificationService.GetUnreadCountAsync(userId, cancellationToken);
        Notifications = await notificationService.GetUserNotificationsAsync(userId, limit: 50, cancellationToken);

        return Page();
    }

    public async Task<IActionResult> OnPostMarkReadAsync(int id, CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdStr, out var userId))
        {
            await notificationService.MarkAsReadAsync(id, userId, cancellationToken);
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostMarkAllReadAsync(CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdStr, out var userId))
        {
            await notificationService.MarkAllAsReadAsync(userId, cancellationToken);
            StatusMessage = "All notifications marked as read.";
        }

        return RedirectToPage();
    }
}
