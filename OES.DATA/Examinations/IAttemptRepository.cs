using OES.MODELS.Examinations;

namespace OES.DATA.Examinations;

public interface IAttemptRepository
{
    Task<ExamAttempt?> GetInProgressAsync(int examId, int studentId, CancellationToken cancellationToken = default);
    Task<ExamAttempt?> GetForExamAsync(int examId, int studentId, CancellationToken cancellationToken = default);
    Task<ExamAttempt?> GetByIdAsync(int attemptId, CancellationToken cancellationToken = default);
    Task<ExamAttempt?> GetByIdForStudentAsync(int attemptId, int studentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StudentAnswer>> GetAnswersAsync(int attemptId, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(int examId, int studentId, DateTime startedUtc, DateTime expiresUtc, CancellationToken cancellationToken = default);
    Task<bool> SubmitAsync(int attemptId, IReadOnlyList<AnswerInput> answers, string status, DateTime submittedUtc, CancellationToken cancellationToken = default);
    Task<bool> MarkTimedOutAsync(int attemptId, DateTime submittedUtc, CancellationToken cancellationToken = default);
}
