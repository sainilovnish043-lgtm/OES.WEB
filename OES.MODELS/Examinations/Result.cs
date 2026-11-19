namespace OES.MODELS.Examinations;

{
}

{
    public int AttemptId { get; init; }
    public int ExamId { get; init; }
    public int StudentId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public string StudentEmail { get; init; } = string.Empty;
    public string ExamTitle { get; init; } = string.Empty;
    public int CorrectAnswers { get; init; }
    public int WrongAnswers { get; init; }
    public decimal TotalMarks { get; init; }
    public decimal PassingMarks { get; init; }
    public decimal ObtainedMarks { get; init; }
    public decimal Percentage { get; init; }
}
