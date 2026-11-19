using Dapper;
using OES.DATA.Database;
using OES.MODELS.Notifications;

namespace OES.DATA.Notifications;

public sealed class NotificationRepository(ISqlConnectionFactory connectionFactory) : INotificationRepository
{
    public async Task<IReadOnlyList<Notification>> GetUserNotificationsAsync(int userId, int limit = 20, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP (@Limit)
                Id, UserId, Title, Message, Type, IsRead, CreatedUtc, ActionUrl
            FROM dbo.Notifications
            WHERE UserId = @UserId
            ORDER BY CreatedUtc DESC;
            """;

        using var connection = connectionFactory.CreateConnection();
        var items = await connection.QueryAsync<Notification>(
            new CommandDefinition(sql, new { UserId = userId, Limit = limit }, cancellationToken: cancellationToken));

        return items.ToList();
    }

    public async Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.Notifications
            WHERE UserId = @UserId AND IsRead = 0;
            """;

        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));
    }

    public async Task<int> CreateAsync(CreateNotificationRequest request, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.Notifications (UserId, Title, Message, Type, IsRead, CreatedUtc, ActionUrl)
            VALUES (@UserId, @Title, @Message, @Type, 0, SYSUTCDATETIME(), @ActionUrl);
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;

        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, request, cancellationToken: cancellationToken));
    }

    public async Task<bool> MarkAsReadAsync(int notificationId, int userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Notifications
            SET IsRead = 1
            WHERE Id = @Id AND UserId = @UserId;
            """;

        using var connection = connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = notificationId, UserId = userId }, cancellationToken: cancellationToken));

        return affected > 0;
    }

    public async Task<int> MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Notifications
            SET IsRead = 1
            WHERE UserId = @UserId AND IsRead = 0;
            """;

        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));
    }

    public async Task<int> NotifyAllStudentsAsync(string title, string message, string type = "info", string? actionUrl = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.Notifications (UserId, Title, Message, Type, IsRead, CreatedUtc, ActionUrl)
            SELECT u.Id, @Title, @Message, @Type, 0, SYSUTCDATETIME(), @ActionUrl
            FROM dbo.Users u
            WHERE u.Role = 'Student' AND u.IsActive = 1;
            """;

        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(
            new CommandDefinition(sql, new { Title = title, Message = message, Type = type, ActionUrl = actionUrl }, cancellationToken: cancellationToken));
    }
}
