namespace OES.MODELS.Examinations;

public sealed class Question
{
    public int Id { get; init; }
    public int ExamId { get; init; }
    public string QuestionText { get; init; } = string.Empty;
    public decimal Marks { get; init; }
    public decimal NegativeMarks { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; }
    public IReadOnlyList<QuestionOption> Options { get; init; } = [];
}

public sealed class QuestionOption
{
    public int Id { get; init; }
    public int QuestionId { get; init; }
    public string OptionText { get; init; } = string.Empty;
    public string OptionKey { get; init; } = string.Empty;
    public bool IsCorrect { get; init; }
    public int DisplayOrder { get; init; }
}

public sealed class QuestionInput
{
    public string QuestionText { get; init; } = string.Empty;
    public decimal Marks { get; init; }
    public decimal NegativeMarks { get; init; }
    public IReadOnlyList<OptionInput> Options { get; init; } = [];
}

public sealed class OptionInput
{
    public string OptionText { get; init; } = string.Empty;
    public bool IsCorrect { get; init; }
}
