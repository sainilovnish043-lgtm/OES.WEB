using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OES.MODELS.Authentication;
using OES.SERVICES.Authentication;

namespace OES.WEB.Pages.Student;

[Authorize(Policy = "StudentOnly")]
public sealed class ProfileModel(IAuthenticationService authenticationService) : PageModel
{
    public StudentProfile? Profile { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdValue, out var userId))
        {
            Profile = await authenticationService.GetStudentProfileAsync(userId, cancellationToken);
        }
    }
}
