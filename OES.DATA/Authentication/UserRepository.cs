using Dapper;
using OES.DATA.Database;
using OES.MODELS.Authentication;

namespace OES.DATA.Authentication;

public sealed class UserRepository(ISqlConnectionFactory connectionFactory) : IUserRepository
{
    public async Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, Email, PasswordHash, Role, IsActive, TwoFactorEnabled
            FROM dbo.Users
            WHERE Email = @Email;
            """;

        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<User>(
            new CommandDefinition(sql, new { Email = email }, cancellationToken: cancellationToken));
    }

    public async Task<User?> FindByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, Email, PasswordHash, Role, IsActive, TwoFactorEnabled
            FROM dbo.Users
            WHERE Id = @Id;
            """;

        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<User>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<Student?> FindStudentByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT s.Id, s.UserId, s.FullName, u.Email, u.IsActive, u.TwoFactorEnabled, s.CreatedUtc
            FROM dbo.Students AS s
            INNER JOIN dbo.Users AS u ON u.Id = s.UserId
            WHERE s.UserId = @UserId;
            """;

        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<StudentProfile>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken)) is { } profile
            ? new Student
            {
                Id = profile.StudentId,
                UserId = profile.UserId,
                FullName = profile.FullName,
                Email = profile.Email,
                IsActive = profile.IsActive,
                CreatedUtc = DateTime.UtcNow
            }
            : null;
    }

    public async Task<IReadOnlyList<Student>> GetStudentsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT s.Id, s.UserId, s.FullName, u.Email, u.IsActive, s.CreatedUtc
            FROM dbo.Students AS s
            INNER JOIN dbo.Users AS u ON u.Id = s.UserId
            ORDER BY s.FullName, s.Id;
            """;

        using var connection = connectionFactory.CreateConnection();
        var students = await connection.QueryAsync<Student>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));
        return students.AsList();
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.Users WHERE Email = @Email) THEN 1 ELSE 0 END AS bit);";
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { Email = email }, cancellationToken: cancellationToken));
    }

    public async Task<int> CreateStudentAsync(string email, string passwordHash, string fullName, CancellationToken cancellationToken = default)
    {
        const string userSql = """
            INSERT INTO dbo.Users (Email, PasswordHash, Role, IsActive, TwoFactorEnabled)
            OUTPUT INSERTED.Id
            VALUES (@Email, @PasswordHash, N'Student', 1, 1);
            """;
        const string studentSql = """
            INSERT INTO dbo.Students (UserId, FullName)
            VALUES (@UserId, @FullName);
            """;

        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            var userId = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                userSql, new { Email = email, PasswordHash = passwordHash }, transaction, cancellationToken: cancellationToken));
            await connection.ExecuteAsync(new CommandDefinition(
                studentSql, new { UserId = userId, FullName = fullName }, transaction, cancellationToken: cancellationToken));
            transaction.Commit();
            return userId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> UpdateStudentStatusAsync(int userId, bool isActive, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Users
            SET IsActive = @IsActive
            WHERE Id = @UserId AND Role = N'Student';
            """;

        using var connection = connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(new CommandDefinition(
            sql, new { UserId = userId, IsActive = isActive }, cancellationToken: cancellationToken));
        return affected == 1;
    }

    public async Task<bool> SetTwoFactorEnabledAsync(int userId, bool enabled, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE dbo.Users SET TwoFactorEnabled = @Enabled WHERE Id = @UserId;";
        using var connection = connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(new CommandDefinition(
            sql, new { UserId = userId, Enabled = enabled }, cancellationToken: cancellationToken));
        return affected == 1;
    }

    public async Task SaveTwoFactorCodeAsync(int userId, string code, DateTime expiresUtc, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.TwoFactorCodes SET IsUsed = 1 WHERE UserId = @UserId AND IsUsed = 0;
            INSERT INTO dbo.TwoFactorCodes (UserId, Code, ExpiresUtc)
            VALUES (@UserId, @Code, @ExpiresUtc);
            """;

        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            sql, new { UserId = userId, Code = code, ExpiresUtc = expiresUtc }, cancellationToken: cancellationToken));
    }

    public async Task<bool> ValidateTwoFactorCodeAsync(int userId, string code, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.TwoFactorCodes
            SET IsUsed = 1
            WHERE Id = (
                SELECT TOP 1 Id
                FROM dbo.TwoFactorCodes
                WHERE UserId = @UserId AND Code = @Code AND IsUsed = 0 AND ExpiresUtc > @UtcNow
                ORDER BY Id DESC
            );
            """;

        using var connection = connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(new CommandDefinition(
            sql, new { UserId = userId, Code = code, UtcNow = utcNow }, cancellationToken: cancellationToken));
        return affected > 0;
    }
}
