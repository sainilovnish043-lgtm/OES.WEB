using Dapper;
using OES.DATA.Database;
using OES.MODELS.Examinations;

namespace OES.DATA.Examinations;

public sealed class ExamRepository(ISqlConnectionFactory connectionFactory) : IExamRepository
{
    public async Task<IReadOnlyList<Exam>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, Title, Description, StartUtc, EndUtc, DurationMinutes,
                   TotalMarks, PassingMarks, Status, IsActive, CreatedBy, CreatedUtc
            FROM dbo.Exams
            ORDER BY StartUtc DESC, Id DESC;
            """;

        using var connection = connectionFactory.CreateConnection();
        var exams = await connection.QueryAsync<Exam>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return exams.AsList();
    }

    public async Task<IReadOnlyList<Exam>> GetAvailableAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, Title, Description, StartUtc, EndUtc, DurationMinutes,
                   TotalMarks, PassingMarks, Status, IsActive, CreatedBy, CreatedUtc
            FROM dbo.Exams
            WHERE IsActive = 1
              AND Status = N'Published'
              AND StartUtc <= @UtcNow
              AND EndUtc > @UtcNow
            ORDER BY StartUtc, Id;
            """;

        using var connection = connectionFactory.CreateConnection();
        var exams = await connection.QueryAsync<Exam>(new CommandDefinition(
            sql, new { UtcNow = utcNow }, cancellationToken: cancellationToken));
        return exams.AsList();
    }

    public async Task<Exam?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, Title, Description, StartUtc, EndUtc, DurationMinutes,
                   TotalMarks, PassingMarks, Status, IsActive, CreatedBy, CreatedUtc
            FROM dbo.Exams
            WHERE Id = @Id;
            """;

        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Exam>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<int> CreateAsync(ExamInput input, int createdBy, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.Exams
                (Title, Description, StartUtc, EndUtc, DurationMinutes, TotalMarks, PassingMarks, Status, IsActive, CreatedBy)
            OUTPUT INSERTED.Id
            VALUES
                (@Title, @Description, @StartUtc, @EndUtc, @DurationMinutes, @TotalMarks, @PassingMarks, N'Draft', 1, @CreatedBy);
            """;

        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            sql,
            new
            {
                input.Title,
                input.Description,
                input.StartUtc,
                input.EndUtc,
                input.DurationMinutes,
                input.TotalMarks,
                input.PassingMarks,
                CreatedBy = createdBy
            },
            cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateAsync(int id, ExamInput input, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Exams
            SET Title = @Title,
                Description = @Description,
                StartUtc = @StartUtc,
                EndUtc = @EndUtc,
                DurationMinutes = @DurationMinutes,
                TotalMarks = @TotalMarks,
                PassingMarks = @PassingMarks
            WHERE Id = @Id AND Status = N'Draft' AND IsActive = 1;
            """;

        using var connection = connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                Id = id,
                input.Title,
                input.Description,
                input.StartUtc,
                input.EndUtc,
                input.DurationMinutes,
                input.TotalMarks,
                input.PassingMarks
            },
            cancellationToken: cancellationToken));
        return affected == 1;
    }

    public async Task<bool> UpdateStatusAsync(int id, string status, bool isActive, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Exams
            SET Status = @Status, IsActive = @IsActive
            WHERE Id = @Id;
            """;

        using var connection = connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(new CommandDefinition(
            sql, new { Id = id, Status = status, IsActive = isActive }, cancellationToken: cancellationToken));
        return affected == 1;
    }
}
