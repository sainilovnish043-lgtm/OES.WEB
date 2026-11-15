using OES.MODELS.Examinations;

namespace OES.SERVICES.Examinations;

public interface IResultService
{
    Task<Result?> GenerateAsync(int attemptId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Result>> GetStudentResultsAsync(int userId, CancellationToken cancellationToken = default);
    Task<Result?> GetStudentResultAsync(int userId, int resultId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Result>> GetAllAsync(int? examId = null, CancellationToken cancellationToken = default);
}
