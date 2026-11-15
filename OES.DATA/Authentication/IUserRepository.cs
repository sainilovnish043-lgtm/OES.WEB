using OES.MODELS.Authentication;

namespace OES.DATA.Authentication;

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Student?> FindStudentByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Student>> GetStudentsAsync(CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task<int> CreateStudentAsync(string email, string passwordHash, string fullName, CancellationToken cancellationToken = default);
    Task<bool> UpdateStudentStatusAsync(int userId, bool isActive, CancellationToken cancellationToken = default);
}
