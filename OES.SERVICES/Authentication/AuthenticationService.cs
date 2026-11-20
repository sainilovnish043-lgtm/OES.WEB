using System.Security.Claims;
using BCrypt.Net;
using OES.DATA.Authentication;
using OES.MODELS.Authentication;

namespace OES.SERVICES.Authentication;

public sealed class AuthenticationService(IUserRepository userRepository) : IAuthenticationService
{
    public async Task<User?> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.FindByEmailAsync(email.Trim(), cancellationToken);

        if (user is null || !user.IsActive || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            return null;
        }

        return user;
    }

    public async Task<User?> GetOrCreateGoogleUserAsync(
        string email,
        string fullName,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await userRepository.FindByEmailAsync(normalizedEmail, cancellationToken);

        if (user is not null)
        {
            return user.IsActive ? user : null;
        }

        var cleanName = string.IsNullOrWhiteSpace(fullName) ? normalizedEmail.Split('@')[0] : fullName.Trim();
        var randomPassword = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N"));
        var newUserId = await userRepository.CreateStudentAsync(normalizedEmail, randomPassword, cleanName, cancellationToken);

        return new User
        {
            Id = newUserId,
            Email = normalizedEmail,
            PasswordHash = randomPassword,
            Role = "Student",
            IsActive = true
        };
    }

    public ClaimsPrincipal CreatePrincipal(User user)
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            ],
            authenticationType: "Cookies");

        return new ClaimsPrincipal(identity);
    }

    public async Task<(bool Succeeded, string? Error)> RegisterStudentAsync(
        StudentRegistration registration,
        CancellationToken cancellationToken = default)
    {
        var email = registration.Email.Trim().ToLowerInvariant();
        var fullName = registration.FullName.Trim();

        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email))
        {
            return (false, "Full name and email are required.");
        }

        if (await userRepository.EmailExistsAsync(email, cancellationToken))
        {
            return (false, "An account with this email already exists.");
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(registration.Password);
        await userRepository.CreateStudentAsync(email, passwordHash, fullName, cancellationToken);
        return (true, null);
    }

    public async Task<StudentProfile?> GetStudentProfileAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.FindByIdAsync(userId, cancellationToken);
        var student = await userRepository.FindStudentByUserIdAsync(userId, cancellationToken);
        return student is null
            ? null
            : new StudentProfile
            {
                UserId = student.UserId,
                StudentId = student.Id,
                FullName = student.FullName,
                Email = student.Email,
                IsActive = student.IsActive,
                TwoFactorEnabled = user?.TwoFactorEnabled ?? true
            };
    }

    public Task<User?> FindUserByIdAsync(int userId, CancellationToken cancellationToken = default) =>
        userRepository.FindByIdAsync(userId, cancellationToken);

    public async Task<string> GenerateTwoFactorCodeAsync(int userId, CancellationToken cancellationToken = default)
    {
        var code = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        var expiresUtc = DateTime.UtcNow.AddMinutes(10);
        await userRepository.SaveTwoFactorCodeAsync(userId, code, expiresUtc, cancellationToken);
        return code;
    }

    public Task<bool> VerifyTwoFactorCodeAsync(int userId, string code, CancellationToken cancellationToken = default)
    {
        var cleanCode = code.Trim();
        return userRepository.ValidateTwoFactorCodeAsync(userId, cleanCode, DateTime.UtcNow, cancellationToken);
    }

    public Task<bool> SetTwoFactorStatusAsync(int userId, bool enabled, CancellationToken cancellationToken = default) =>
        userRepository.SetTwoFactorEnabledAsync(userId, enabled, cancellationToken);

    public Task<IReadOnlyList<Student>> GetStudentsAsync(CancellationToken cancellationToken = default) =>
        userRepository.GetStudentsAsync(cancellationToken);

    public Task<bool> UpdateStudentStatusAsync(int userId, bool isActive, CancellationToken cancellationToken = default) =>
        userRepository.UpdateStudentStatusAsync(userId, isActive, cancellationToken);
}
