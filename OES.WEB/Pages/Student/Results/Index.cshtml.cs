using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OES.MODELS.Examinations;
using OES.SERVICES.Examinations;

namespace OES.WEB.Pages.Student.Results;

[Authorize(Policy = "StudentOnly")]
public sealed class IndexModel(IResultService resultService) : PageModel
{
    public IReadOnlyList<StudentWiseResultItem> Results { get; private set; } = [];
    public int TotalExamsTaken => Results.Count;
    public int PassedCount => Results.Count(r => r.IsPassed);
    public int FailedCount => TotalExamsTaken - PassedCount;
    public decimal OverallAverage => TotalExamsTaken > 0 ? Math.Round(Results.Average(r => r.Percentage), 2) : 0;

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Forbid();
        }

        Results = await resultService.GetStudentResultsAsync(userId, cancellationToken);
        return Page();
    }
}
