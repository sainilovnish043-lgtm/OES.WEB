using Dapper;
using OES.DATA.Database;
using OES.MODELS.Examinations;

namespace OES.DATA.Examinations;

public sealed class AttemptRepository(ISqlConnectionFactory connectionFactory) : IAttemptRepository
{
    public async Task<ExamAttempt?> GetInProgressAsync(int examId, int studentId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, ExamId, StudentId, StartedUtc, ExpiresUtc, SubmittedUtc, Status
            FROM dbo.ExamAttempts
            WHERE ExamId = @ExamId AND StudentId = @StudentId AND Status = N'InProgress';
            """;
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<ExamAttempt>(new CommandDefinition(
            sql, new { ExamId = examId, StudentId = studentId }, cancellationToken: cancellationToken));
    }

    public async Task<ExamAttempt?> GetForExamAsync(int examId, int studentId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, ExamId, StudentId, StartedUtc, ExpiresUtc, SubmittedUtc, Status
            FROM dbo.ExamAttempts
            WHERE ExamId = @ExamId AND StudentId = @StudentId;
            """;
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<ExamAttempt>(new CommandDefinition(
            sql, new { ExamId = examId, StudentId = studentId }, cancellationToken: cancellationToken));
    }

    {
        const string sql = """
            SELECT Id, ExamId, StudentId, StartedUtc, ExpiresUtc, SubmittedUtc, Status
            FROM dbo.ExamAttempts
            """;
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<ExamAttempt>(new CommandDefinition(
    }

    {
        const string sql = """
            SELECT Id, ExamId, StudentId, StartedUtc, ExpiresUtc, SubmittedUtc, Status
            FROM dbo.ExamAttempts
            """;
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<ExamAttempt>(new CommandDefinition(
    }

    public async Task<IReadOnlyList<StudentAnswer>> GetAnswersAsync(int attemptId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, AttemptId, QuestionId, SelectedOptionId, AnsweredUtc
            FROM dbo.StudentAnswers
            WHERE AttemptId = @AttemptId;
            """;
        using var connection = connectionFactory.CreateConnection();
        var answers = await connection.QueryAsync<StudentAnswer>(new CommandDefinition(
            sql, new { AttemptId = attemptId }, cancellationToken: cancellationToken));
        return answers.AsList();
    }

    public async Task<int> CreateAsync(int examId, int studentId, DateTime startedUtc, DateTime expiresUtc, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.ExamAttempts (ExamId, StudentId, StartedUtc, ExpiresUtc, Status)
            OUTPUT INSERTED.Id
            VALUES (@ExamId, @StudentId, @StartedUtc, @ExpiresUtc, N'InProgress');
            """;
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            sql, new { ExamId = examId, StudentId = studentId, StartedUtc = startedUtc, ExpiresUtc = expiresUtc },
            cancellationToken: cancellationToken));
    }

    public async Task<bool> SubmitAsync(
        int attemptId,
        IReadOnlyList<AnswerInput> answers,
        string status,
        DateTime submittedUtc,
        CancellationToken cancellationToken = default)
    {
        const string deleteSql = "DELETE FROM dbo.StudentAnswers WHERE AttemptId = @AttemptId;";
        const string answerSql = """
            INSERT INTO dbo.StudentAnswers (AttemptId, QuestionId, SelectedOptionId, AnsweredUtc)
            VALUES (@AttemptId, @QuestionId, @SelectedOptionId, @AnsweredUtc);
            """;
        const string updateSql = """
            UPDATE dbo.ExamAttempts
            SET Status = @Status, SubmittedUtc = @SubmittedUtc
            WHERE Id = @AttemptId AND Status = N'InProgress';
            """;

        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            await connection.ExecuteAsync(new CommandDefinition(
                deleteSql, new { AttemptId = attemptId }, transaction, cancellationToken: cancellationToken));

            foreach (var answer in answers.Where(answer => answer.SelectedOptionId.HasValue))
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    answerSql,
                    new
                    {
                        AttemptId = attemptId,
                        answer.QuestionId,
                        SelectedOptionId = answer.SelectedOptionId!.Value,
                        AnsweredUtc = submittedUtc
                    },
                    transaction,
                    cancellationToken: cancellationToken));
            }

            var affected = await connection.ExecuteAsync(new CommandDefinition(
                updateSql,
                new { AttemptId = attemptId, Status = status, SubmittedUtc = submittedUtc },
                transaction,
                cancellationToken: cancellationToken));
            if (affected != 1)
            {
                transaction.Rollback();
                return false;
            }

            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> MarkTimedOutAsync(int attemptId, DateTime submittedUtc, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.ExamAttempts
            SET Status = N'TimedOut', SubmittedUtc = @SubmittedUtc
            WHERE Id = @AttemptId AND Status = N'InProgress';
            """;
        using var connection = connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(new CommandDefinition(
            sql, new { AttemptId = attemptId, SubmittedUtc = submittedUtc }, cancellationToken: cancellationToken));
        return affected == 1;
    }
}
