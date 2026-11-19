using Dapper;
using OES.DATA.Database;
using OES.MODELS.Examinations;

namespace OES.DATA.Examinations;

public sealed class ResultRepository(ISqlConnectionFactory connectionFactory) : IResultRepository
{
            """;

            {
    }

    {
        const string sql = """
            SELECT Id, AttemptId, ExamId, StudentId, TotalMarks, PassingMarks, ObtainedMarks, Percentage,
                   CorrectAnswers, WrongAnswers, UnattemptedQuestions, IsPassed, CalculatedUtc
            FROM dbo.Results
            WHERE AttemptId = @AttemptId;
            """;

        using var connection = connectionFactory.CreateConnection();
    }

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
        return results.AsList();
    }

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
        return results.AsList();
    }

    {
        const string sql = """
            """;

        using var connection = connectionFactory.CreateConnection();
    {
    }
}
