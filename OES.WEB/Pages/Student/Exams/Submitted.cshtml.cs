using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OES.MODELS.Ai;
using OES.MODELS.Examinations;
using OES.SERVICES.Ai;
using OES.SERVICES.Examinations;

namespace OES.WEB.Pages.Student.Exams;

[Authorize(Policy = "StudentOnly")]
public sealed class SubmittedModel(
    IResultService resultService,
    IExamService examService,
    IAiService aiService) : PageModel
{
    public string Status { get; private set; } = AttemptStatuses.Submitted;
    public ExamResult? Result { get; private set; }
    public AiStudyRecommendation? AiRecommendation { get; private set; }

    public async Task<IActionResult> OnGetAsync(int? attemptId, string? status, CancellationToken cancellationToken)
    {
        Status = status is AttemptStatuses.TimedOut ? AttemptStatuses.TimedOut : AttemptStatuses.Submitted;

        if (attemptId.HasValue && int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            Result = await resultService.GetResultByAttemptAsync(attemptId.Value, userId, cancellationToken);
            if (Result is not null)
            {
                var exam = await examService.GetByIdAsync(Result.ExamId, cancellationToken);
                AiRecommendation = await aiService.GenerateStudyRecommendationsAsync(
                    exam?.Title ?? "Exam",
                    Result.ObtainedMarks,
                    Result.TotalMarks,
                    Result.Percentage,
                    Result.IsPassed,
                    Result.CorrectAnswers,
                    Result.WrongAnswers,
                    Result.UnattemptedQuestions,
                    cancellationToken);
            }
        }

        return Page();
    }
}
