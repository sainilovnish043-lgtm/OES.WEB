using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OES.MODELS.Examinations;
using OES.SERVICES.Examinations;

namespace OES.WEB.Pages.Admin.Results;

[Authorize(Policy = "AdminOnly")]
{
    public IReadOnlyList<Result> Results { get; private set; } = [];
    public IReadOnlyList<Exam> Exams { get; private set; } = [];

    {
        SelectedExamId = examId;
        Exams = await examService.GetAllAsync(cancellationToken);
    }
}
