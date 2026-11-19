using OES.MODELS.Authentication;

namespace OES.DATA.Authentication;

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> FindByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Student?> FindStudentByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Student>> GetStudentsAsync(CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task<int> CreateStudentAsync(string email, string passwordHash, string fullName, CancellationToken cancellationToken = default);
    Task<bool> UpdateStudentStatusAsync(int userId, bool isActive, CancellationToken cancellationToken = default);
    Task<bool> SetTwoFactorEnabledAsync(int userId, bool enabled, CancellationToken cancellationToken = default);
    Task SaveTwoFactorCodeAsync(int userId, string code, DateTime expiresUtc, CancellationToken cancellationToken = default);
    Task<bool> ValidateTwoFactorCodeAsync(int userId, string code, DateTime utcNow, CancellationToken cancellationToken = default);
}
