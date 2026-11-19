using OES.MODELS.Notifications;

namespace OES.DATA.Notifications;

public interface INotificationRepository
{
    Task<IReadOnlyList<Notification>> GetUserNotificationsAsync(int userId, int limit = 20, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(CreateNotificationRequest request, CancellationToken cancellationToken = default);
    Task<bool> MarkAsReadAsync(int notificationId, int userId, CancellationToken cancellationToken = default);
    Task<int> MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default);
    Task<int> NotifyAllStudentsAsync(string title, string message, string type = "info", string? actionUrl = null, CancellationToken cancellationToken = default);
}
