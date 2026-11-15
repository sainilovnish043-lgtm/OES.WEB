using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OES.MODELS.Examinations;
using OES.SERVICES.Examinations;

namespace OES.WEB.Pages.Admin.Exams.Questions;

[Authorize(Policy = "AdminOnly")]
public sealed class IndexModel(IQuestionService questionService, IExamService examService) : PageModel
{
    public Exam? Exam { get; private set; }
    public IReadOnlyList<Question> Questions { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int examId, CancellationToken cancellationToken)
    {
        Exam = await examService.GetByIdAsync(examId, cancellationToken);
        if (Exam is null)
        {
            return NotFound();
        }

        Questions = await questionService.GetByExamIdAsync(examId, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int examId, int id, CancellationToken cancellationToken)
    {
        await questionService.DeleteAsync(id, cancellationToken);
        return RedirectToPage(new { examId });
    }
}
