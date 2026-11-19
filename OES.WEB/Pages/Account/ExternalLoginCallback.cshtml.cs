using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using IAuthService = OES.SERVICES.Authentication.IAuthenticationService;

namespace OES.WEB.Pages.Account;

public sealed class ExternalLoginCallbackModel(IAuthService authenticationService) : PageModel
{
    public async Task<IActionResult> OnGetAsync(string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        var authResult = await HttpContext.AuthenticateAsync("ExternalCookie");
        if (!authResult.Succeeded || authResult.Principal is null)
        {
            return RedirectToPage("Login", new { error = "Google authentication failed or was cancelled." });
        }

        var email = authResult.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            return RedirectToPage("Login", new { error = "Unable to retrieve email from your Google account." });
        }

        var fullName = authResult.Principal.FindFirstValue(ClaimTypes.Name)
            ?? authResult.Principal.FindFirstValue(ClaimTypes.GivenName)
            ?? email.Split('@')[0];

        var user = await authenticationService.GetOrCreateGoogleUserAsync(email, fullName, cancellationToken);
        if (user is null)
        {
            return RedirectToPage("Login", new { error = "Your account has been deactivated. Please contact administrator." });
        }

        // Clean up temporary external cookie
        await HttpContext.SignOutAsync("ExternalCookie");

        // Sign into application cookie
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            authenticationService.CreatePrincipal(user));

        return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
    }
}
