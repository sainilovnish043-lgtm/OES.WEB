using Dapper;
using OES.DATA.Database;
using OES.MODELS.Examinations;

namespace OES.DATA.Examinations;

public sealed class ResultRepository(ISqlConnectionFactory connectionFactory) : IResultRepository
{
    public async Task<int> SaveResultAsync(ExamResult result, CancellationToken cancellationToken = default)
    {
        const string sql = """
            IF EXISTS (SELECT 1 FROM dbo.Results WHERE AttemptId = @AttemptId)
            BEGIN
                UPDATE dbo.Results
                SET ExamId = @ExamId,
                    StudentId = @StudentId,
                    TotalMarks = @TotalMarks,
                    PassingMarks = @PassingMarks,
                    ObtainedMarks = @ObtainedMarks,
                    Percentage = @Percentage,
                    CorrectAnswers = @CorrectAnswers,
                    WrongAnswers = @WrongAnswers,
                    UnattemptedQuestions = @UnattemptedQuestions,
                    IsPassed = @IsPassed,
                    CalculatedUtc = @CalculatedUtc
                WHERE AttemptId = @AttemptId;

                SELECT Id FROM dbo.Results WHERE AttemptId = @AttemptId;
            END
            ELSE
            BEGIN
                INSERT INTO dbo.Results
                    (AttemptId, ExamId, StudentId, TotalMarks, PassingMarks, ObtainedMarks, Percentage,
                     CorrectAnswers, WrongAnswers, UnattemptedQuestions, IsPassed, CalculatedUtc)
                OUTPUT INSERTED.Id
                VALUES
                    (@AttemptId, @ExamId, @StudentId, @TotalMarks, @PassingMarks, @ObtainedMarks, @Percentage,
                     @CorrectAnswers, @WrongAnswers, @UnattemptedQuestions, @IsPassed, @CalculatedUtc);
            END
            """;

        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            sql,
            new
            {
                result.AttemptId,
                result.ExamId,
                result.StudentId,
                result.TotalMarks,
                result.PassingMarks,
                result.ObtainedMarks,
                result.Percentage,
                result.CorrectAnswers,
                result.WrongAnswers,
                result.UnattemptedQuestions,
                result.IsPassed,
                result.CalculatedUtc
            },
            cancellationToken: cancellationToken));
    }

    public async Task<ExamResult?> GetByAttemptIdAsync(int attemptId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, AttemptId, ExamId, StudentId, TotalMarks, PassingMarks, ObtainedMarks, Percentage,
                   CorrectAnswers, WrongAnswers, UnattemptedQuestions, IsPassed, CalculatedUtc
            FROM dbo.Results
            WHERE AttemptId = @AttemptId;
            """;

        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<ExamResult>(new CommandDefinition(
            sql, new { AttemptId = attemptId }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<ExamWiseResultItem>> GetByExamIdAsync(int examId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT r.Id AS ResultId, r.AttemptId, r.ExamId, r.StudentId,
                   s.FullName AS StudentName, u.Email AS StudentEmail, e.Title AS ExamTitle,
                   r.TotalMarks, r.PassingMarks, r.ObtainedMarks, r.Percentage,
                   r.CorrectAnswers, r.WrongAnswers, r.UnattemptedQuestions, r.IsPassed,
                   ISNULL(a.SubmittedUtc, a.ExpiresUtc) AS SubmittedUtc, r.CalculatedUtc
            FROM dbo.Results r
            INNER JOIN dbo.Students s ON r.StudentId = s.Id
            INNER JOIN dbo.Users u ON s.UserId = u.Id
            INNER JOIN dbo.Exams e ON r.ExamId = e.Id
            INNER JOIN dbo.ExamAttempts a ON r.AttemptId = a.Id
            WHERE r.ExamId = @ExamId
            ORDER BY r.ObtainedMarks DESC, r.CalculatedUtc ASC;
            """;

        using var connection = connectionFactory.CreateConnection();
        var results = await connection.QueryAsync<ExamWiseResultItem>(new CommandDefinition(
            sql, new { ExamId = examId }, cancellationToken: cancellationToken));
        return results.AsList();
    }

    public async Task<IReadOnlyList<StudentWiseResultItem>> GetByStudentIdAsync(int studentId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT r.Id AS ResultId, r.AttemptId, r.ExamId, e.Title AS ExamTitle,
                   r.StudentId, s.FullName AS StudentName,
                   r.TotalMarks, r.PassingMarks, r.ObtainedMarks, r.Percentage,
                   r.CorrectAnswers, r.WrongAnswers, r.UnattemptedQuestions, r.IsPassed,
                   ISNULL(a.SubmittedUtc, a.ExpiresUtc) AS SubmittedUtc
            FROM dbo.Results r
            INNER JOIN dbo.Exams e ON r.ExamId = e.Id
            INNER JOIN dbo.Students s ON r.StudentId = s.Id
            INNER JOIN dbo.ExamAttempts a ON r.AttemptId = a.Id
            WHERE r.StudentId = @StudentId
            ORDER BY r.CalculatedUtc DESC;
            """;

        using var connection = connectionFactory.CreateConnection();
        var results = await connection.QueryAsync<StudentWiseResultItem>(new CommandDefinition(
            sql, new { StudentId = studentId }, cancellationToken: cancellationToken));
        return results.AsList();
    }

    public async Task<IReadOnlyList<ExamWiseResultItem>> GetAllResultsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT r.Id AS ResultId, r.AttemptId, r.ExamId, r.StudentId,
                   s.FullName AS StudentName, u.Email AS StudentEmail, e.Title AS ExamTitle,
                   r.TotalMarks, r.PassingMarks, r.ObtainedMarks, r.Percentage,
                   r.CorrectAnswers, r.WrongAnswers, r.UnattemptedQuestions, r.IsPassed,
                   ISNULL(a.SubmittedUtc, a.ExpiresUtc) AS SubmittedUtc, r.CalculatedUtc
            FROM dbo.Results r
            INNER JOIN dbo.Students s ON r.StudentId = s.Id
            INNER JOIN dbo.Users u ON s.UserId = u.Id
            INNER JOIN dbo.Exams e ON r.ExamId = e.Id
            INNER JOIN dbo.ExamAttempts a ON r.AttemptId = a.Id
            ORDER BY r.CalculatedUtc DESC;
            """;

        using var connection = connectionFactory.CreateConnection();
        var results = await connection.QueryAsync<ExamWiseResultItem>(new CommandDefinition(
            sql, cancellationToken: cancellationToken));
        return results.AsList();
    }

    public async Task<ResultSummary> GetExamSummaryAsync(int examId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                COUNT(1) AS TotalAttempts,
                ISNULL(SUM(CASE WHEN IsPassed = 1 THEN 1 ELSE 0 END), 0) AS PassedCount,
                ISNULL(SUM(CASE WHEN IsPassed = 0 THEN 1 ELSE 0 END), 0) AS FailedCount,
                ISNULL(AVG(ObtainedMarks), 0) AS AverageMarks,
                ISNULL(MAX(ObtainedMarks), 0) AS HighestMarks
            FROM dbo.Results
            WHERE ExamId = @ExamId;
            """;

        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<ResultSummary>(new CommandDefinition(
            sql, new { ExamId = examId }, cancellationToken: cancellationToken)) ?? new ResultSummary();
    }
}
