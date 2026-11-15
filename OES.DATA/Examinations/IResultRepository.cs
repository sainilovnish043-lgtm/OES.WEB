using OES.MODELS.Examinations;

namespace OES.DATA.Examinations;

public interface IResultRepository
{
    Task<Result?> GetByAttemptIdAsync(int attemptId, CancellationToken cancellationToken = default);
    Task<Result?> GetByIdForStudentAsync(int resultId, int studentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Result>> GetByStudentIdAsync(int studentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Result>> GetAllAsync(int? examId = null, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(Result result, CancellationToken cancellationToken = default);
}
