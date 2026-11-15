using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OES.MODELS.Examinations;
using OES.SERVICES.Examinations;

namespace OES.WEB.Pages.Admin.Exams;

[Authorize(Policy = "AdminOnly")]
public sealed class IndexModel(IExamService examService) : PageModel
{
    public IReadOnlyList<Exam> Exams { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Exams = await examService.GetAllAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostPublishAsync(int id, CancellationToken cancellationToken)
    {
        await examService.PublishAsync(id, cancellationToken);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUnpublishAsync(int id, CancellationToken cancellationToken)
    {
        await examService.UnpublishAsync(id, cancellationToken);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeactivateAsync(int id, CancellationToken cancellationToken)
    {
        await examService.DeactivateAsync(id, cancellationToken);
        return RedirectToPage();
    }
}
