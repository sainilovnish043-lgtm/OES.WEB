namespace OES.MODELS.Examinations;

public sealed class ExamResult
{
    public int Id { get; init; }
    public int AttemptId { get; init; }
    public int ExamId { get; init; }
    public int StudentId { get; init; }
    public decimal TotalMarks { get; init; }
    public decimal PassingMarks { get; init; }
    public decimal ObtainedMarks { get; init; }
    public decimal Percentage { get; init; }
    public int CorrectAnswers { get; init; }
    public int WrongAnswers { get; init; }
    public int UnattemptedQuestions { get; init; }
    public bool IsPassed { get; init; }
    public DateTime CalculatedUtc { get; init; }
}

public sealed class ExamWiseResultItem
{
    public int ResultId { get; init; }
    public int AttemptId { get; init; }
    public int ExamId { get; init; }
    public int StudentId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public string StudentEmail { get; init; } = string.Empty;
    public string ExamTitle { get; init; } = string.Empty;
    public decimal TotalMarks { get; init; }
    public decimal PassingMarks { get; init; }
    public decimal ObtainedMarks { get; init; }
    public decimal Percentage { get; init; }
    public int CorrectAnswers { get; init; }
    public int WrongAnswers { get; init; }
    public int UnattemptedQuestions { get; init; }
    public bool IsPassed { get; init; }
    public DateTime SubmittedUtc { get; init; }
    public DateTime CalculatedUtc { get; init; }
}

public sealed class StudentWiseResultItem
{
    public int ResultId { get; init; }
    public int AttemptId { get; init; }
    public int ExamId { get; init; }
    public string ExamTitle { get; init; } = string.Empty;
    public int StudentId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public decimal TotalMarks { get; init; }
    public decimal PassingMarks { get; init; }
    public decimal ObtainedMarks { get; init; }
    public decimal Percentage { get; init; }
    public int CorrectAnswers { get; init; }
    public int WrongAnswers { get; init; }
    public int UnattemptedQuestions { get; init; }
    public bool IsPassed { get; init; }
    public DateTime SubmittedUtc { get; init; }
}

public sealed class ResultSummary
{
    public int TotalAttempts { get; init; }
    public int PassedCount { get; init; }
    public int FailedCount { get; init; }
    public decimal AverageMarks { get; init; }
    public decimal HighestMarks { get; init; }
    public decimal PassPercentage => TotalAttempts > 0 ? Math.Round((decimal)PassedCount / TotalAttempts * 100, 2) : 0;
}
