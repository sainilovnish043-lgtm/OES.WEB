namespace OES.MODELS.Notifications;

public sealed class Notification
{
    public int Id { get; init; }
    public int UserId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Type { get; init; } = "info"; // info, success, warning, danger
    public bool IsRead { get; init; }
    public DateTime CreatedUtc { get; init; }
    public string? ActionUrl { get; init; }
}

public sealed class CreateNotificationRequest
{
    public int UserId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Type { get; init; } = "info";
    public string? ActionUrl { get; init; }
}
