using OES.MODELS.Examinations;

namespace OES.DATA.Examinations;

public interface IQuestionRepository
{
    Task<IReadOnlyList<Question>> GetByExamIdAsync(int examId, CancellationToken cancellationToken = default);
    Task<Question?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(int examId, QuestionInput input, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, QuestionInput input, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
