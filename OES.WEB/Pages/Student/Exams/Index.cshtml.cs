using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OES.MODELS.Examinations;
using OES.SERVICES.Examinations;

namespace OES.WEB.Pages.Student.Exams;

[Authorize(Policy = "StudentOnly")]
public sealed class IndexModel(IAttemptService attemptService) : PageModel
{
    public IReadOnlyList<Exam> Exams { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        if (TryGetUserId(out var userId))
        {
            Exams = await attemptService.GetAvailableExamsAsync(userId, DateTime.UtcNow, cancellationToken);
        }
    }

    public async Task<IActionResult> OnPostStartAsync(int examId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Forbid();
        }

        var result = await attemptService.StartAsync(userId, examId, DateTime.UtcNow, cancellationToken);
        if (!result.Succeeded || result.AttemptId is null)
        {
            TempData["ErrorMessage"] = result.Error ?? "The exam could not be started.";
            return RedirectToPage();
        }

        return RedirectToPage("Attempt", new { attemptId = result.AttemptId.Value });
    }

    private bool TryGetUserId(out int userId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
