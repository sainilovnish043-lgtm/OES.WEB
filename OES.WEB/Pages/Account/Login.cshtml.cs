using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OES.SERVICES.Authentication;
using AuthenticationService = OES.SERVICES.Authentication.IAuthenticationService;

namespace OES.WEB.Pages.Account;

public sealed class LoginModel(
    AuthenticationService authenticationService,
    IConfiguration configuration) : PageModel
{
    [BindProperty]
    public LoginInput Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public void OnGet(string? error = null)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            ModelState.AddModelError(string.Empty, error);
        }
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await authenticationService.ValidateCredentialsAsync(
            Input.Email,
            Input.Password,
            cancellationToken);

        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return Page();
        }

        if (user.TwoFactorEnabled)
        {
            var code = await authenticationService.GenerateTwoFactorCodeAsync(user.Id, cancellationToken);
            var identity = new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role)
                ],
                "TwoFactorCookie");

            await HttpContext.SignInAsync("TwoFactorCookie", new ClaimsPrincipal(identity));
            TempData["DemoTwoFactorCode"] = code;

            return RedirectToPage("VerifyTwoFactor", new { returnUrl = ReturnUrl });
        }

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            authenticationService.CreatePrincipal(user));

        return LocalRedirect(string.IsNullOrWhiteSpace(ReturnUrl) ? "/" : ReturnUrl);
    }

    public IActionResult OnPostGoogle(string? returnUrl = null)
    {
        var clientId = configuration["Authentication:Google:ClientId"] ?? configuration["web:client_id"];
        if (string.IsNullOrWhiteSpace(clientId) || clientId.Contains("YOUR_GOOGLE_CLIENT_ID", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(string.Empty, "Google Sign-In is enabled! Please set your Google ClientId and ClientSecret in appsettings.json to connect to Google.");
            return Page();
        }

        var redirectUrl = Url.Page("/Account/ExternalLoginCallback", pageHandler: null, values: new { returnUrl });
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    public sealed class LoginInput
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
