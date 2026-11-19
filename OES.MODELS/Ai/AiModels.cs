namespace OES.MODELS.Ai;

public sealed class AiGenerateQuestionsRequest
{
    public string Topic { get; set; } = string.Empty;
    public string Difficulty { get; set; } = "Medium"; // Easy, Medium, Hard
    public int Count { get; set; } = 5;
    public decimal Marks { get; set; } = 1;
    public decimal NegativeMarks { get; set; } = 0;
}

public sealed class AiGeneratedQuestion
{
    public string QuestionText { get; set; } = string.Empty;
    public List<AiGeneratedOption> Options { get; set; } = [];
    public string? Explanation { get; set; }
}

public sealed class AiGeneratedOption
{
    public string OptionText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}

public sealed class AiStudyRecommendation
{
    public string OverallFeedback { get; set; } = string.Empty;
    public List<string> Strengths { get; set; } = [];
    public List<string> AreasToImprove { get; set; } = [];
    public List<string> StudyTips { get; set; } = [];
}
