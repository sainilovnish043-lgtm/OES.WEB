using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using IAuthService = OES.SERVICES.Authentication.IAuthenticationService;

namespace OES.WEB.Pages.Account;

public sealed class VerifyTwoFactorModel(IAuthService authenticationService) : PageModel
{
    [BindProperty]
    [Required(ErrorMessage = "Verification code is required.")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Code must be exactly 6 digits.")]
    [RegularExpression(@"^[0-9]{6}$", ErrorMessage = "Code must be 6 numeric digits.")]
    public string Code { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? DemoCode { get; private set; }
    public string UserEmail { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        var authResult = await HttpContext.AuthenticateAsync("TwoFactorCookie");
        if (!authResult.Succeeded || authResult.Principal is null)
        {
            return RedirectToPage("Login");
        }

        UserEmail = authResult.Principal.FindFirstValue(ClaimTypes.Email) ?? "your email";
        DemoCode = TempData["DemoTwoFactorCode"] as string;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var authResult = await HttpContext.AuthenticateAsync("TwoFactorCookie");
        if (!authResult.Succeeded || authResult.Principal is null)
        {
            return RedirectToPage("Login", new { error = "Session expired. Please log in again." });
        }

        var userIdString = authResult.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdString, out var userId))
        {
            return RedirectToPage("Login");
        }

        UserEmail = authResult.Principal.FindFirstValue(ClaimTypes.Email) ?? "your email";

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var isValid = await authenticationService.VerifyTwoFactorCodeAsync(userId, Code, cancellationToken);
        if (!isValid)
        {
            ModelState.AddModelError(nameof(Code), "Invalid or expired verification code. Please check and try again.");
            return Page();
        }

        var user = await authenticationService.FindUserByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return RedirectToPage("Login", new { error = "Account is inactive." });
        }

        await HttpContext.SignOutAsync("TwoFactorCookie");

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            authenticationService.CreatePrincipal(user));

        return LocalRedirect(string.IsNullOrWhiteSpace(ReturnUrl) ? "/" : ReturnUrl);
    }

    public async Task<IActionResult> OnPostResendAsync(CancellationToken cancellationToken)
    {
        var authResult = await HttpContext.AuthenticateAsync("TwoFactorCookie");
        if (!authResult.Succeeded || authResult.Principal is null)
        {
            return RedirectToPage("Login");
        }

        var userIdString = authResult.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdString, out var userId))
        {
            var newCode = await authenticationService.GenerateTwoFactorCodeAsync(userId, cancellationToken);
            TempData["DemoTwoFactorCode"] = newCode;
            TempData["ResendSuccess"] = "A new verification code has been generated.";
        }

        return RedirectToPage("VerifyTwoFactor", new { returnUrl = ReturnUrl });
    }
}
