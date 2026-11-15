using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OES.MODELS.Authentication;
using OES.SERVICES.Authentication;

namespace OES.WEB.Pages.Account;

public sealed class RegisterModel(IAuthenticationService authenticationService) : PageModel
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

    public sealed class RegistrationInput
    {
        [Required, StringLength(150, MinimumLength = 2)]
        [Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(100, MinimumLength = 8), DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required, Compare(nameof(Password)), DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
