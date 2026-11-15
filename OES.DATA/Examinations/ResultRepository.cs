using Dapper;
using OES.DATA.Database;
using OES.MODELS.Examinations;

namespace OES.DATA.Examinations;

public sealed class ResultRepository(ISqlConnectionFactory connectionFactory) : IResultRepository
{
    private const string SelectSql = """
        SELECT r.Id, r.AttemptId, r.ExamId, r.StudentId,
               e.Title AS ExamTitle, s.FullName AS StudentName,
               r.TotalQuestions, r.CorrectAnswers, r.WrongAnswers,
               r.UnansweredQuestions, r.TotalMarks, r.ObtainedMarks,
               r.Percentage, r.PassingMarks, r.Status, r.GeneratedUtc
        FROM dbo.Results AS r
        INNER JOIN dbo.Exams AS e ON e.Id = r.ExamId
        INNER JOIN dbo.Students AS s ON s.Id = r.StudentId
        """;

    public async Task<Result?> GetByAttemptIdAsync(int attemptId, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Result>(new CommandDefinition(
            $"{SelectSql} WHERE r.AttemptId = @AttemptId;",
            new { AttemptId = attemptId }, cancellationToken: cancellationToken));
    }

    public async Task<Result?> GetByIdForStudentAsync(int resultId, int studentId, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Result>(new CommandDefinition(
            $"{SelectSql} WHERE r.Id = @ResultId AND r.StudentId = @StudentId;",
            new { ResultId = resultId, StudentId = studentId }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<Result>> GetByStudentIdAsync(int studentId, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        var results = await connection.QueryAsync<Result>(new CommandDefinition(
            $"{SelectSql} WHERE r.StudentId = @StudentId ORDER BY r.GeneratedUtc DESC, r.Id DESC;",
            new { StudentId = studentId }, cancellationToken: cancellationToken));
        return results.AsList();
    }

    public async Task<IReadOnlyList<Result>> GetAllAsync(int? examId = null, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        var results = await connection.QueryAsync<Result>(new CommandDefinition(
            $"{SelectSql} WHERE @ExamId IS NULL OR r.ExamId = @ExamId ORDER BY r.GeneratedUtc DESC, r.Id DESC;",
            new { ExamId = examId }, cancellationToken: cancellationToken));
        return results.AsList();
    }

    public async Task<int> CreateAsync(Result result, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.Results
                (AttemptId, ExamId, StudentId, TotalQuestions, CorrectAnswers, WrongAnswers,
                 UnansweredQuestions, TotalMarks, ObtainedMarks, Percentage, PassingMarks, Status)
            OUTPUT INSERTED.Id
            VALUES
                (@AttemptId, @ExamId, @StudentId, @TotalQuestions, @CorrectAnswers, @WrongAnswers,
                 @UnansweredQuestions, @TotalMarks, @ObtainedMarks, @Percentage, @PassingMarks, @Status);
            """;
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            sql,
            new
            {
                result.AttemptId,
                result.ExamId,
                result.StudentId,
                result.TotalQuestions,
                result.CorrectAnswers,
                result.WrongAnswers,
                result.UnansweredQuestions,
                result.TotalMarks,
                result.ObtainedMarks,
                result.Percentage,
                result.PassingMarks,
                result.Status
            },
            cancellationToken: cancellationToken));
    }
}
