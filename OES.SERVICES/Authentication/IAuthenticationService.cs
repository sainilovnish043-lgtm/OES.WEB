using System.Security.Claims;
using OES.MODELS.Authentication;

namespace OES.SERVICES.Authentication;

public interface IAuthenticationService
{
    Task<User?> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<User?> FindUserByIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<User?> GetOrCreateGoogleUserAsync(string email, string fullName, CancellationToken cancellationToken = default);
    ClaimsPrincipal CreatePrincipal(User user);
    Task<(bool Succeeded, string? Error)> RegisterStudentAsync(StudentRegistration registration, CancellationToken cancellationToken = default);
    Task<StudentProfile?> GetStudentProfileAsync(int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Student>> GetStudentsAsync(CancellationToken cancellationToken = default);
    Task<bool> UpdateStudentStatusAsync(int userId, bool isActive, CancellationToken cancellationToken = default);
    Task<string> GenerateTwoFactorCodeAsync(int userId, CancellationToken cancellationToken = default);
    Task<bool> VerifyTwoFactorCodeAsync(int userId, string code, CancellationToken cancellationToken = default);
    Task<bool> SetTwoFactorStatusAsync(int userId, bool enabled, CancellationToken cancellationToken = default);
}
