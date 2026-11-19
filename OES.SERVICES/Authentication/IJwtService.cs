namespace OES.SERVICES.Authentication;

public interface IJwtService
{
    string GenerateToken(int userId, string email, string role, TimeSpan? expiry = null);
    bool ValidateToken(string token, out int userId, out string role);
}
