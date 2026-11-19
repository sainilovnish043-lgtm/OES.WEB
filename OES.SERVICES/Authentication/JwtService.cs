using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace OES.SERVICES.Authentication;

public sealed class JwtService : IJwtService
{
    private readonly byte[] _key;
    private readonly string _issuer;
    private readonly string _audience;

    public JwtService(string secretKey, string issuer = "OES.WEB", string audience = "OES.Clients")
    {
        if (string.IsNullOrWhiteSpace(secretKey) || secretKey.Length < 16)
        {
            throw new ArgumentException("A JWT signing key of at least 16 characters is required.", nameof(secretKey));
        }

        _key = Encoding.UTF8.GetBytes(secretKey);
        _issuer = issuer;
        _audience = audience;
    }

    public string GenerateToken(int userId, string email, string role, TimeSpan? expiry = null)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var expires = DateTime.UtcNow.Add(expiry ?? TimeSpan.FromHours(8));

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, email),
            new(ClaimTypes.Role, role),
            new("role", role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(_key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public bool ValidateToken(string token, out int userId, out string role)
    {
        userId = 0;
        role = string.Empty;

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(_key),
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateAudience = true,
            ValidAudience = _audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };

        try
        {
            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            if (validatedToken is not JwtSecurityToken)
            {
                return false;
            }

            var idClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roleClaim = principal.FindFirst(ClaimTypes.Role)?.Value ?? principal.FindFirst("role")?.Value ?? string.Empty;

            if (int.TryParse(idClaim, out userId))
            {
                role = roleClaim;
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}
