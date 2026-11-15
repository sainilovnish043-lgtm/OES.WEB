using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OES.MODELS.Examinations;
using OES.SERVICES.Examinations;

namespace OES.WEB.Pages.Student.Results;

[Authorize(Policy = "StudentOnly")]
public sealed class IndexModel(IResultService resultService) : PageModel
{
    public IReadOnlyList<Result> Results { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            Results = await resultService.GetStudentResultsAsync(userId, cancellationToken);
        }
    }
}
