using OES.MODELS.Examinations;

namespace OES.SERVICES.Examinations;

public interface IResultService
{
    Task<ExamResult?> CalculateAndSaveResultAsync(int attemptId, CancellationToken cancellationToken = default);
    Task<ExamResult?> GetResultByAttemptAsync(int attemptId, int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExamWiseResultItem>> GetExamResultsAsync(int examId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StudentWiseResultItem>> GetStudentResultsAsync(int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExamWiseResultItem>> GetAllResultsAsync(CancellationToken cancellationToken = default);
    Task<ResultSummary> GetExamSummaryAsync(int examId, CancellationToken cancellationToken = default);
}
