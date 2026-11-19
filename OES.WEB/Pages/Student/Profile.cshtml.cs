using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    public async Task<IActionResult> OnPostToggleTwoFactorAsync(bool enable, CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdValue, out var userId))
        {
            await authenticationService.SetTwoFactorStatusAsync(userId, enable, cancellationToken);
            TempData["ProfileMessage"] = enable
                ? "Two-Factor Authentication (2FA) is now ENABLED for your account."
                : "Two-Factor Authentication (2FA) is now DISABLED for your account.";
        }

        return RedirectToPage();
    }
}
