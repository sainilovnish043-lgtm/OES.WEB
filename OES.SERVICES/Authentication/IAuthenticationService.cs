using System.Security.Claims;
using OES.MODELS.Authentication;

namespace OES.SERVICES.Authentication;

public interface IAuthenticationService
{
    Task<User?> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default);
    ClaimsPrincipal CreatePrincipal(User user);
    Task<(bool Succeeded, string? Error)> RegisterStudentAsync(StudentRegistration registration, CancellationToken cancellationToken = default);
    Task<StudentProfile?> GetStudentProfileAsync(int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Student>> GetStudentsAsync(CancellationToken cancellationToken = default);
    Task<bool> UpdateStudentStatusAsync(int userId, bool isActive, CancellationToken cancellationToken = default);
}
