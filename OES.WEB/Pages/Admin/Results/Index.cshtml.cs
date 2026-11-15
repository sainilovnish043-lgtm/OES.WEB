using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OES.MODELS.Examinations;
using OES.SERVICES.Examinations;

namespace OES.WEB.Pages.Admin.Results;

[Authorize(Policy = "AdminOnly")]
public sealed class IndexModel(IResultService resultService, IExamService examService) : PageModel
{
    public IReadOnlyList<Result> Results { get; private set; } = [];
    public IReadOnlyList<Exam> Exams { get; private set; } = [];
    public int? SelectedExamId { get; private set; }

    public async Task OnGetAsync(int? examId, CancellationToken cancellationToken)
    {
        SelectedExamId = examId;
        Exams = await examService.GetAllAsync(cancellationToken);
        Results = await resultService.GetAllAsync(examId, cancellationToken);
    }
}
