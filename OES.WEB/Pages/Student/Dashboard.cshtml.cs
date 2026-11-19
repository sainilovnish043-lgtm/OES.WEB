using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OES.MODELS.Authentication;
using OES.MODELS.Examinations;
using OES.SERVICES.Authentication;
using OES.SERVICES.Examinations;

namespace OES.WEB.Pages.Student;

[Authorize(Policy = "StudentOnly")]
public sealed class DashboardModel(
    IAuthenticationService authenticationService,
    IResultService resultService,
    IAttemptService attemptService) : PageModel
{
    public StudentProfile? Profile { get; private set; }
    public IReadOnlyList<StudentWiseResultItem> Results { get; private set; } = [];
    public IReadOnlyList<Exam> AvailableExams { get; private set; } = [];

    public int TotalExamsAttempted => Results.Count;
    public int PassedCount => Results.Count(r => r.IsPassed);
    public int FailedCount => TotalExamsAttempted - PassedCount;
    public decimal OverallAveragePercentage => TotalExamsAttempted > 0
        ? Math.Round(Results.Average(r => r.Percentage), 1)
        : 0;

    // Charts data
    public string ExamTitlesJson { get; private set; } = "[]";
    public string ScoresJson { get; private set; } = "[]";
    public string PassFailJson { get; private set; } = "[0, 0]";

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId))
        {
            return Forbid();
        }

        Profile = await authenticationService.GetStudentProfileAsync(userId, cancellationToken);
        Results = await resultService.GetStudentResultsAsync(userId, cancellationToken);
        AvailableExams = await attemptService.GetAvailableExamsAsync(userId, DateTime.UtcNow, cancellationToken);

        // Chart 1: Progress Trend across recent exams
        var chartResults = Results.Take(8).Reverse().ToList();
        if (chartResults.Count > 0)
        {
            ExamTitlesJson = JsonSerializer.Serialize(chartResults.Select(r => r.ExamTitle));
            ScoresJson = JsonSerializer.Serialize(chartResults.Select(r => r.Percentage));
        }

        // Chart 2: Pass vs Fail Ratio
        PassFailJson = JsonSerializer.Serialize(new[] { PassedCount, FailedCount });

        return Page();
    }
}
