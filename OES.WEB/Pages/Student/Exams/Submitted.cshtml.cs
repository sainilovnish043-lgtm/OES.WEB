using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OES.MODELS.Examinations;

namespace OES.WEB.Pages.Student.Exams;

[Authorize(Policy = "StudentOnly")]
public sealed class SubmittedModel : PageModel
{
    public string Status { get; private set; } = AttemptStatuses.Submitted;

    public IActionResult OnGet(string? status)
    {
        Status = status is AttemptStatuses.TimedOut ? AttemptStatuses.TimedOut : AttemptStatuses.Submitted;
        return Page();
    }
}
