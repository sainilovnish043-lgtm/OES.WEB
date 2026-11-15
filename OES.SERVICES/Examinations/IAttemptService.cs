using OES.MODELS.Examinations;

namespace OES.SERVICES.Examinations;

public interface IAttemptService
{
    Task<IReadOnlyList<Exam>> GetAvailableExamsAsync(int userId, DateTime utcNow, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error, int? AttemptId)> StartAsync(int userId, int examId, DateTime utcNow, CancellationToken cancellationToken = default);
    Task<AttemptSession?> GetSessionAsync(int userId, int attemptId, DateTime utcNow, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error)> SubmitAsync(int userId, int attemptId, IReadOnlyList<AnswerInput> answers, DateTime utcNow, CancellationToken cancellationToken = default);
}
