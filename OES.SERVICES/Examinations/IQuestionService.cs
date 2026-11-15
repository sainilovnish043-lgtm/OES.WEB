using OES.MODELS.Examinations;

namespace OES.SERVICES.Examinations;

public interface IQuestionService
{
    Task<IReadOnlyList<Question>> GetByExamIdAsync(int examId, CancellationToken cancellationToken = default);
    Task<Question?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error, int? QuestionId)> CreateAsync(int examId, QuestionInput input, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error)> UpdateAsync(int id, QuestionInput input, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
