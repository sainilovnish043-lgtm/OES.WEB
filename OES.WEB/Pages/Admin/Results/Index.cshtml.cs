using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OES.MODELS.Examinations;
using OES.SERVICES.Examinations;

namespace OES.WEB.Pages.Admin.Results;

[Authorize(Policy = "AdminOnly")]
public sealed class IndexModel(
    IResultService resultService,
    IExamService examService) : PageModel
{
    public IReadOnlyList<Exam> Exams { get; private set; } = [];
    public IReadOnlyList<ExamWiseResultItem> Results { get; private set; } = [];
    public ResultSummary Summary { get; private set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? ExamId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Exams = await examService.GetAllAsync(cancellationToken);

        IReadOnlyList<ExamWiseResultItem> allResults;
        if (ExamId.HasValue && ExamId.Value > 0)
        {
            allResults = await resultService.GetExamResultsAsync(ExamId.Value, cancellationToken);
            Summary = await resultService.GetExamSummaryAsync(ExamId.Value, cancellationToken);
        }
        else
        {
            allResults = await resultService.GetAllResultsAsync(cancellationToken);
            Summary = CalculateSummary(allResults);
        }

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var term = SearchTerm.Trim();
            Results = allResults
                .Where(r => r.StudentName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                            r.StudentEmail.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                            r.ExamTitle.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        else
        {
            Results = allResults;
        }
    }

    private static ResultSummary CalculateSummary(IReadOnlyList<ExamWiseResultItem> results)
    {
        if (results.Count == 0)
        {
            return new ResultSummary();
        }

        var passed = results.Count(r => r.IsPassed);
        var failed = results.Count - passed;
        var avg = results.Average(r => r.ObtainedMarks);
        var highest = results.Max(r => r.ObtainedMarks);

        return new ResultSummary
        {
            TotalAttempts = results.Count,
            PassedCount = passed,
            FailedCount = failed,
            AverageMarks = Math.Round(avg, 2),
            HighestMarks = highest
        };
    }
}
