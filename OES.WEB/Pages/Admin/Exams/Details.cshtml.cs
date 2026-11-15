using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OES.MODELS.Examinations;
using OES.SERVICES.Examinations;

namespace OES.WEB.Pages.Admin.Exams;

[Authorize(Policy = "AdminOnly")]
public sealed class DetailsModel(IExamService examService) : PageModel
{
    public Exam? Exam { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        Exam = await examService.GetByIdAsync(id, cancellationToken);
        return Exam is null ? NotFound() : Page();
    }
}
