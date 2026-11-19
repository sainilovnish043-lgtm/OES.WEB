using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OES.SERVICES.Authentication;
using IAuthService = OES.SERVICES.Authentication.IAuthenticationService;

namespace OES.WEB.Pages.Api;

[IgnoreAntiforgeryToken]
public sealed class TokenModel(IAuthService authService, IJwtService jwtService) : PageModel
{
    public sealed record TokenRequest(string? Email, string? Password);
    public sealed record TokenResponse(string Token, string Role, int UserId, string Email, DateTime ExpiresUtc);

    public IActionResult OnGet()
    {
        return new JsonResult(new
        {
            message = "Send a POST request with JSON { email, password } to obtain a JWT token.",
            endpoint = "/api/token"
        });
    }

    public async Task<IActionResult> OnPostAsync([FromBody] TokenRequest? request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { error = "Email and password are required." });
        }

        var user = await authService.ValidateCredentialsAsync(request.Email.Trim(), request.Password, cancellationToken);
        if (user is null)
        {
            return StatusCode(StatusCodes.Status401Unauthorized, new { error = "Invalid email or password." });
        }

        var expiry = TimeSpan.FromHours(8);
        var token = jwtService.GenerateToken(user.Id, user.Email, user.Role, expiry);

        return new JsonResult(new TokenResponse(
            Token: token,
            Role: user.Role,
            UserId: user.Id,
            Email: user.Email,
            ExpiresUtc: DateTime.UtcNow.Add(expiry)
        ));
    }
}
