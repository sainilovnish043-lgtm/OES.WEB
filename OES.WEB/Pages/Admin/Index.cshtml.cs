using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OES.MODELS.Examinations;
using OES.SERVICES.Authentication;
using OES.SERVICES.Examinations;

namespace OES.WEB.Pages.Admin;

[Authorize(Policy = "AdminOnly")]
public sealed class IndexModel(
    IAuthenticationService authenticationService,
    IExamService examService,
    IResultService resultService) : PageModel
{
    public int TotalStudents { get; private set; }
    public int TotalExams { get; private set; }
    public int PublishedExamsCount { get; private set; }
    public int DraftExamsCount { get; private set; }
    public int TotalSubmissions { get; private set; }
    public int PassedCount { get; private set; }
    public int FailedCount { get; private set; }
    public decimal PassRate { get; private set; }

    public IReadOnlyList<ExamWiseResultItem> RecentResults { get; private set; } = [];

    // Chart.js serialized JSON
    public string ExamLabelsJson { get; private set; } = "[]";
    public string ExamAvgScoresJson { get; private set; } = "[]";
    public string ExamPassingScoresJson { get; private set; } = "[]";
    public string PassFailDataJson { get; private set; } = "[0, 0]";

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var students = await authenticationService.GetStudentsAsync(cancellationToken);
        var exams = await examService.GetAllAsync(cancellationToken);
        var results = await resultService.GetAllResultsAsync(cancellationToken);

        TotalStudents = students.Count;
        TotalExams = exams.Count;
        PublishedExamsCount = exams.Count(e => e.Status == ExamStatuses.Published);
        DraftExamsCount = exams.Count(e => e.Status == ExamStatuses.Draft);

        TotalSubmissions = results.Count;
        PassedCount = results.Count(r => r.IsPassed);
        FailedCount = TotalSubmissions - PassedCount;
        PassRate = TotalSubmissions > 0 ? Math.Round((decimal)PassedCount / TotalSubmissions * 100, 1) : 0;

        RecentResults = results.Take(6).ToList();

        // Chart 1: Exam Average Scores vs Passing Marks
        var examPerformance = results
            .GroupBy(r => new { r.ExamId, r.ExamTitle, r.PassingMarks })
            .Select(g => new
            {
                Title = g.Key.ExamTitle,
                Average = Math.Round(g.Average(r => r.ObtainedMarks), 1),
                Passing = g.Key.PassingMarks
            })
            .Take(8)
            .ToList();

        if (examPerformance.Count > 0)
        {
            ExamLabelsJson = JsonSerializer.Serialize(examPerformance.Select(e => e.Title));
            ExamAvgScoresJson = JsonSerializer.Serialize(examPerformance.Select(e => e.Average));
            ExamPassingScoresJson = JsonSerializer.Serialize(examPerformance.Select(e => e.Passing));
        }
        else if (exams.Count > 0)
        {
            // Fallback to exam titles with 0 average if no attempts yet
            var sampleExams = exams.Take(6).ToList();
            ExamLabelsJson = JsonSerializer.Serialize(sampleExams.Select(e => e.Title));
            ExamAvgScoresJson = JsonSerializer.Serialize(sampleExams.Select(_ => 0));
            ExamPassingScoresJson = JsonSerializer.Serialize(sampleExams.Select(e => e.PassingMarks));
        }

        // Chart 2: Pass vs Fail Doughnut
        PassFailDataJson = JsonSerializer.Serialize(new[] { PassedCount, FailedCount });
    }
}
