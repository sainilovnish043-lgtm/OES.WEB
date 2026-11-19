using OES.MODELS.Notifications;

namespace OES.SERVICES.Notifications;

public interface INotificationService
{
    Task<IReadOnlyList<Notification>> GetUserNotificationsAsync(int userId, int limit = 20, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default);
    Task<int> NotifyUserAsync(int userId, string title, string message, string type = "info", string? actionUrl = null, CancellationToken cancellationToken = default);
    Task<int> BroadcastToStudentsAsync(string title, string message, string type = "info", string? actionUrl = null, CancellationToken cancellationToken = default);
    Task<bool> MarkAsReadAsync(int notificationId, int userId, CancellationToken cancellationToken = default);
    Task<int> MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default);
}
