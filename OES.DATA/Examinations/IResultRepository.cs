using OES.MODELS.Examinations;

namespace OES.DATA.Examinations;

public interface IResultRepository
{
    Task<int> SaveResultAsync(ExamResult result, CancellationToken cancellationToken = default);
    Task<ExamResult?> GetByAttemptIdAsync(int attemptId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExamWiseResultItem>> GetByExamIdAsync(int examId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StudentWiseResultItem>> GetByStudentIdAsync(int studentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExamWiseResultItem>> GetAllResultsAsync(CancellationToken cancellationToken = default);
    Task<ResultSummary> GetExamSummaryAsync(int examId, CancellationToken cancellationToken = default);
}
