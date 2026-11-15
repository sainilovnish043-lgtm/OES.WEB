namespace OES.MODELS.Examinations;

public static class ExamStatuses
{
    public const string Draft = "Draft";
    public const string Published = "Published";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
}

public sealed class Exam
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime StartUtc { get; init; }
    public DateTime EndUtc { get; init; }
    public int DurationMinutes { get; init; }
    public decimal TotalMarks { get; init; }
    public decimal PassingMarks { get; init; }
    public string Status { get; init; } = ExamStatuses.Draft;
    public bool IsActive { get; init; }
    public int CreatedBy { get; init; }
    public DateTime CreatedUtc { get; init; }
}

public sealed class ExamInput
{
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime StartUtc { get; init; }
    public DateTime EndUtc { get; init; }
    public int DurationMinutes { get; init; }
    public decimal TotalMarks { get; init; }
    public decimal PassingMarks { get; init; }
}
