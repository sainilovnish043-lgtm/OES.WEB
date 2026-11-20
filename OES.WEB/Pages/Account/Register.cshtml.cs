using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OES.MODELS.Authentication;
using IAuthService = OES.SERVICES.Authentication.IAuthenticationService;

namespace OES.WEB.Pages.Account;

public sealed class RegisterModel(
    IAuthService authenticationService,
    IConfiguration configuration) : PageModel
{
    [BindProperty]
    public RegistrationInput Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await authenticationService.RegisterStudentAsync(
            new StudentRegistration
            {
                FullName = Input.FullName,
                Email = Input.Email,
                Password = Input.Password
            },
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Registration failed.");
            return Page();
        }

        TempData["SuccessMessage"] = "Registration successful. You can now sign in.";
        return RedirectToPage("/Account/Login");
    }

    public IActionResult OnPostGoogle()
    {
        var clientId = configuration["Authentication:Google:ClientId"] ?? configuration["web:client_id"];
        if (string.IsNullOrWhiteSpace(clientId) || clientId.Contains("YOUR_GOOGLE_CLIENT_ID", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(string.Empty, "Google Sign-In is enabled! Please set your Google ClientId and ClientSecret in appsettings.json to connect to Google.");
            return Page();
        }

        var redirectUrl = Url.Page("/Account/ExternalLoginCallback", pageHandler: null);
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    public sealed class RegistrationInput
    {
        [Required, StringLength(150, MinimumLength = 2)]
        [Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(100, MinimumLength = 8)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required, Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
