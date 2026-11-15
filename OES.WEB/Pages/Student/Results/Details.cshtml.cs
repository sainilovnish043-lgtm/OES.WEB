using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OES.MODELS.Examinations;
using OES.SERVICES.Examinations;

namespace OES.WEB.Pages.Student.Results;

[Authorize(Policy = "StudentOnly")]
public sealed class DetailsModel(IResultService resultService) : PageModel
{
    public Result? Result { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Forbid();
        }

        Result = await resultService.GetStudentResultAsync(userId, id, cancellationToken);
        return Result is null ? NotFound() : Page();
    }
}
