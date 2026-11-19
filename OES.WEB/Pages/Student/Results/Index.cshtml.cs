using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OES.MODELS.Examinations;
using OES.SERVICES.Examinations;

namespace OES.WEB.Pages.Student.Results;

[Authorize(Policy = "StudentOnly")]
public sealed class IndexModel(IResultService resultService) : PageModel
{

    {
        {
        }

        Results = await resultService.GetStudentResultsAsync(userId, cancellationToken);
        return Page();
    }
}
