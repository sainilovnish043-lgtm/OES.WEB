using OES.DATA.Examinations;
using OES.MODELS.Examinations;

namespace OES.SERVICES.Examinations;

public sealed class QuestionService(
    IQuestionRepository questionRepository,
    IExamRepository examRepository) : IQuestionService
{
    public Task<IReadOnlyList<Question>> GetByExamIdAsync(int examId, CancellationToken cancellationToken = default) =>
        questionRepository.GetByExamIdAsync(examId, cancellationToken);

    public Task<Question?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        questionRepository.GetByIdAsync(id, cancellationToken);

    public async Task<(bool Succeeded, string? Error, int? QuestionId)> CreateAsync(
        int examId,
        QuestionInput input,
        CancellationToken cancellationToken = default)
    {
        var examError = await ValidateExamAsync(examId, cancellationToken);
        if (examError is not null)
        {
            return (false, examError, null);
        }

        var validationError = ValidateInput(input);
        if (validationError is not null)
        {
            return (false, validationError, null);
        }

        var questionId = await questionRepository.CreateAsync(examId, input, cancellationToken);
        return (true, null, questionId);
    }

    public async Task<(bool Succeeded, string? Error)> UpdateAsync(
        int id,
        QuestionInput input,
        CancellationToken cancellationToken = default)
    {
        var question = await questionRepository.GetByIdAsync(id, cancellationToken);
        if (question is null)
        {
            return (false, "Question was not found.");
        }

        var examError = await ValidateExamAsync(question.ExamId, cancellationToken);
        if (examError is not null)
        {
            return (false, examError);
        }

        var validationError = ValidateInput(input);
        if (validationError is not null)
        {
            return (false, validationError);
        }

        var updated = await questionRepository.UpdateAsync(id, input, cancellationToken);
        return updated ? (true, null) : (false, "Question could not be updated.");
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var question = await questionRepository.GetByIdAsync(id, cancellationToken);
        if (question is null || await ValidateExamAsync(question.ExamId, cancellationToken) is not null)
        {
            return false;
        }

        return await questionRepository.DeleteAsync(id, cancellationToken);
    }

    private async Task<string?> ValidateExamAsync(int examId, CancellationToken cancellationToken)
    {
        var exam = await examRepository.GetByIdAsync(examId, cancellationToken);
        return exam switch
        {
            null => "Exam was not found.",
            { IsActive: false } => "Questions cannot be changed for an inactive exam.",
            { Status: not ExamStatuses.Draft } => "Questions can only be changed while the exam is a draft.",
            _ => null
        };
    }

    private static string? ValidateInput(QuestionInput input)
    {
        if (string.IsNullOrWhiteSpace(input.QuestionText))
        {
            return "Question text is required.";
        }

        if (input.Marks <= 0)
        {
            return "Question marks must be greater than zero.";
        }

        if (input.NegativeMarks < 0)
        {
            return "Negative marks cannot be less than zero.";
        }

        if (input.Options.Count is < 2 or > 6)
        {
            return "A question must have between 2 and 6 options.";
        }

        if (input.Options.Any(option => string.IsNullOrWhiteSpace(option.OptionText)))
        {
            return "Every option must contain text.";
        }

        if (input.Options.Count(option => option.IsCorrect) != 1)
        {
            return "Exactly one option must be marked as correct.";
        }

        return null;
    }
}
