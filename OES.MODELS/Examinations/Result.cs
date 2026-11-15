namespace OES.MODELS.Examinations;

public static class ResultStatuses
{
    public const string Pass = "Pass";
    public const string Fail = "Fail";
}

public sealed class Result
{
    public int Id { get; init; }
    public int AttemptId { get; init; }
    public int ExamId { get; init; }
    public int StudentId { get; init; }
    public string ExamTitle { get; init; } = string.Empty;
    public string StudentName { get; init; } = string.Empty;
    public int TotalQuestions { get; init; }
    public int CorrectAnswers { get; init; }
    public int WrongAnswers { get; init; }
    public int UnansweredQuestions { get; init; }
    public decimal TotalMarks { get; init; }
    public decimal ObtainedMarks { get; init; }
    public decimal Percentage { get; init; }
    public decimal PassingMarks { get; init; }
    public string Status { get; init; } = ResultStatuses.Fail;
    public DateTime GeneratedUtc { get; init; }
}
