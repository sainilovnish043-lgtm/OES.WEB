using OES.DATA.Notifications;
using OES.MODELS.Notifications;

namespace OES.SERVICES.Notifications;

public sealed class NotificationService(INotificationRepository notificationRepository) : INotificationService
{
    public Task<IReadOnlyList<Notification>> GetUserNotificationsAsync(int userId, int limit = 20, CancellationToken cancellationToken = default)
        => notificationRepository.GetUserNotificationsAsync(userId, limit, cancellationToken);

    public Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default)
        => notificationRepository.GetUnreadCountAsync(userId, cancellationToken);

    public Task<int> NotifyUserAsync(int userId, string title, string message, string type = "info", string? actionUrl = null, CancellationToken cancellationToken = default)
    {
        return notificationRepository.CreateAsync(new CreateNotificationRequest
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            ActionUrl = actionUrl
        }, cancellationToken);
    }

    public Task<int> BroadcastToStudentsAsync(string title, string message, string type = "info", string? actionUrl = null, CancellationToken cancellationToken = default)
        => notificationRepository.NotifyAllStudentsAsync(title, message, type, actionUrl, cancellationToken);

    public Task<bool> MarkAsReadAsync(int notificationId, int userId, CancellationToken cancellationToken = default)
        => notificationRepository.MarkAsReadAsync(notificationId, userId, cancellationToken);

    public Task<int> MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default)
        => notificationRepository.MarkAllAsReadAsync(userId, cancellationToken);
}
