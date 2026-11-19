using OES.MODELS.Ai;

namespace OES.SERVICES.Ai;

public interface IAiService
{
    Task<IReadOnlyList<AiGeneratedQuestion>> GenerateQuestionsAsync(
        AiGenerateQuestionsRequest request,
        CancellationToken cancellationToken = default);

    Task<AiStudyRecommendation> GenerateStudyRecommendationsAsync(
        string examTitle,
        decimal obtainedMarks,
        decimal totalMarks,
        decimal percentage,
        bool isPassed,
        int correctCount,
        int wrongCount,
        int unattemptedCount,
        CancellationToken cancellationToken = default);
}
