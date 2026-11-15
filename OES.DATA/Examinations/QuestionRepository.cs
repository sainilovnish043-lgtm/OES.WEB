using Dapper;
using OES.DATA.Database;
using OES.MODELS.Examinations;

namespace OES.DATA.Examinations;

public sealed class QuestionRepository(ISqlConnectionFactory connectionFactory) : IQuestionRepository
{
    public async Task<IReadOnlyList<Question>> GetByExamIdAsync(int examId, CancellationToken cancellationToken = default)
    {
        const string questionSql = """
            SELECT Id, ExamId, QuestionText, Marks, NegativeMarks, DisplayOrder, IsActive
            FROM dbo.Questions
            WHERE ExamId = @ExamId AND IsActive = 1
            ORDER BY DisplayOrder, Id;
            """;
        const string optionSql = """
            SELECT Id, QuestionId, OptionText, OptionKey, IsCorrect, DisplayOrder
            FROM dbo.Options
            WHERE QuestionId IN (SELECT Id FROM dbo.Questions WHERE ExamId = @ExamId AND IsActive = 1)
            ORDER BY QuestionId, DisplayOrder, Id;
            """;

        using var connection = connectionFactory.CreateConnection();
        var questions = (await connection.QueryAsync<Question>(
            new CommandDefinition(questionSql, new { ExamId = examId }, cancellationToken: cancellationToken))).AsList();
        if (questions.Count == 0)
        {
            return questions;
        }

        var options = (await connection.QueryAsync<QuestionOption>(
            new CommandDefinition(optionSql, new { ExamId = examId }, cancellationToken: cancellationToken)))
            .GroupBy(option => option.QuestionId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<QuestionOption>)group.ToList());

        return questions.Select(question => new Question
        {
            Id = question.Id,
            ExamId = question.ExamId,
            QuestionText = question.QuestionText,
            Marks = question.Marks,
            NegativeMarks = question.NegativeMarks,
            DisplayOrder = question.DisplayOrder,
            IsActive = question.IsActive,
            Options = options.GetValueOrDefault(question.Id, [])
        }).ToList();
    }

    public async Task<Question?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string questionSql = """
            SELECT Id, ExamId, QuestionText, Marks, NegativeMarks, DisplayOrder, IsActive
            FROM dbo.Questions
            WHERE Id = @Id;
            """;
        const string optionSql = """
            SELECT Id, QuestionId, OptionText, OptionKey, IsCorrect, DisplayOrder
            FROM dbo.Options
            WHERE QuestionId = @Id
            ORDER BY DisplayOrder, Id;
            """;

        using var connection = connectionFactory.CreateConnection();
        var question = await connection.QuerySingleOrDefaultAsync<Question>(
            new CommandDefinition(questionSql, new { Id = id }, cancellationToken: cancellationToken));
        if (question is null)
        {
            return null;
        }

        var options = await connection.QueryAsync<QuestionOption>(
            new CommandDefinition(optionSql, new { Id = id }, cancellationToken: cancellationToken));
        return new Question
        {
            Id = question.Id,
            ExamId = question.ExamId,
            QuestionText = question.QuestionText,
            Marks = question.Marks,
            NegativeMarks = question.NegativeMarks,
            DisplayOrder = question.DisplayOrder,
            IsActive = question.IsActive,
            Options = options.AsList()
        };
    }

    public async Task<int> CreateAsync(int examId, QuestionInput input, CancellationToken cancellationToken = default)
    {
        const string questionSql = """
            INSERT INTO dbo.Questions (ExamId, QuestionText, Marks, NegativeMarks, DisplayOrder, IsActive)
            OUTPUT INSERTED.Id
            VALUES (@ExamId, @QuestionText, @Marks, @NegativeMarks,
                    (SELECT COALESCE(MAX(DisplayOrder), 0) + 1 FROM dbo.Questions WHERE ExamId = @ExamId), 1);
            """;

        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            var questionId = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                questionSql,
                new { ExamId = examId, input.QuestionText, input.Marks, input.NegativeMarks },
                transaction,
                cancellationToken: cancellationToken));
            await InsertOptionsAsync(connection, transaction, questionId, input.Options, cancellationToken);
            transaction.Commit();
            return questionId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> UpdateAsync(int id, QuestionInput input, CancellationToken cancellationToken = default)
    {
        const string questionSql = """
            UPDATE dbo.Questions
            SET QuestionText = @QuestionText, Marks = @Marks, NegativeMarks = @NegativeMarks
            WHERE Id = @Id AND IsActive = 1;
            """;
        const string deleteOptionsSql = "DELETE FROM dbo.Options WHERE QuestionId = @QuestionId;";

        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            var affected = await connection.ExecuteAsync(new CommandDefinition(
                questionSql,
                new { Id = id, input.QuestionText, input.Marks, input.NegativeMarks },
                transaction,
                cancellationToken: cancellationToken));
            if (affected != 1)
            {
                transaction.Rollback();
                return false;
            }

            await connection.ExecuteAsync(new CommandDefinition(
                deleteOptionsSql, new { QuestionId = id }, transaction, cancellationToken: cancellationToken));
            await InsertOptionsAsync(connection, transaction, id, input.Options, cancellationToken);
            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE dbo.Questions SET IsActive = 0 WHERE Id = @Id AND IsActive = 1;";
        using var connection = connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(new CommandDefinition(
            sql, new { Id = id }, cancellationToken: cancellationToken));
        return affected == 1;
    }

    private static async Task InsertOptionsAsync(
        System.Data.IDbConnection connection,
        System.Data.IDbTransaction transaction,
        int questionId,
        IReadOnlyList<OptionInput> options,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.Options (QuestionId, OptionText, OptionKey, IsCorrect, DisplayOrder)
            VALUES (@QuestionId, @OptionText, @OptionKey, @IsCorrect, @DisplayOrder);
            """;

        for (var index = 0; index < options.Count; index++)
        {
            var option = options[index];
            await connection.ExecuteAsync(new CommandDefinition(
                sql,
                new
                {
                    QuestionId = questionId,
                    OptionText = option.OptionText,
                    OptionKey = ((char)('A' + index)).ToString(),
                    option.IsCorrect,
                    DisplayOrder = index + 1
                },
                transaction,
                cancellationToken: cancellationToken));
        }
    }
}
