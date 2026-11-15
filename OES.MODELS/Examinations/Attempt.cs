namespace OES.MODELS.Examinations;

public static class AttemptStatuses
{
    public const string InProgress = "InProgress";
    public const string Submitted = "Submitted";
    public const string TimedOut = "TimedOut";
}

public sealed class ExamAttempt
{
    public int Id { get; init; }
    public int ExamId { get; init; }
    public int StudentId { get; init; }
    public DateTime StartedUtc { get; init; }
    public DateTime ExpiresUtc { get; init; }
    public DateTime? SubmittedUtc { get; init; }
    public string Status { get; init; } = AttemptStatuses.InProgress;
}

public sealed class StudentAnswer
{
    public int Id { get; init; }
    public int AttemptId { get; init; }
    public int QuestionId { get; init; }
    public int SelectedOptionId { get; init; }
    public DateTime AnsweredUtc { get; init; }
}

public sealed class AnswerInput
{
    public int QuestionId { get; init; }
    public int? SelectedOptionId { get; init; }
}

public sealed class AttemptSession
{
    public ExamAttempt Attempt { get; init; } = new();
    public Exam Exam { get; init; } = new();
    public IReadOnlyList<Question> Questions { get; init; } = [];
    public IReadOnlyList<StudentAnswer> Answers { get; init; } = [];
}
