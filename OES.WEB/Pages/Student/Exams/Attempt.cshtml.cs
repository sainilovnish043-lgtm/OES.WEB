using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OES.MODELS.Examinations;
using OES.SERVICES.Examinations;

namespace OES.WEB.Pages.Student.Exams;

[Authorize(Policy = "StudentOnly")]
public sealed class AttemptModel(IAttemptService attemptService) : PageModel
{
    [BindProperty]
    public List<AnswerForm> Answers { get; set; } = [];

    public AttemptSession? Session { get; private set; }
    public long ExpiresUnixMilliseconds => Session is null
        ? 0
        : new DateTimeOffset(DateTime.SpecifyKind(Session.Attempt.ExpiresUtc, DateTimeKind.Utc)).ToUnixTimeMilliseconds();

    public async Task<IActionResult> OnGetAsync(int attemptId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Forbid();
        }

        Session = await attemptService.GetSessionAsync(userId, attemptId, DateTime.UtcNow, cancellationToken);
        if (Session is null)
        {
            return NotFound();
        }

        SetAnswerForms();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int attemptId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Forbid();
        }

        Session = await attemptService.GetSessionAsync(userId, attemptId, DateTime.UtcNow, cancellationToken);
        if (Session is null)
        {
            return NotFound();
        }

        if (Session.Attempt.Status != AttemptStatuses.InProgress)
        {
            return RedirectToPage("Submitted", new { attemptId, status = Session.Attempt.Status });
        }

        var result = await attemptService.SubmitAsync(
            userId,
            attemptId,
            Answers.Select(answer => new AnswerInput
            {
                QuestionId = answer.QuestionId,
                SelectedOptionId = answer.SelectedOptionId
            }).ToList(),
            DateTime.UtcNow,
            cancellationToken);

        if (result.Succeeded)
        {
            return RedirectToPage("Submitted", new { attemptId, status = AttemptStatuses.Submitted });
        }

        if (result.Error?.StartsWith("The exam time expired", StringComparison.Ordinal) == true)
        {
            return RedirectToPage("Submitted", new { attemptId, status = AttemptStatuses.TimedOut });
        }

        ModelState.AddModelError(string.Empty, result.Error ?? "The exam could not be submitted.");
        SetAnswerForms();
        return Page();
    }

    private void SetAnswerForms()
    {
        var savedAnswers = Session!.Answers.ToDictionary(answer => answer.QuestionId, answer => (int?)answer.SelectedOptionId);
        Answers = Session.Questions.Select(question => new AnswerForm
        {
            QuestionId = question.Id,
            SelectedOptionId = savedAnswers.GetValueOrDefault(question.Id)
        }).ToList();
    }

    private bool TryGetUserId(out int userId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    public sealed class AnswerForm
    {
        public int QuestionId { get; set; }
        public int? SelectedOptionId { get; set; }
    }
}
