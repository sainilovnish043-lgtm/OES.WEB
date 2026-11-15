using OES.MODELS.Examinations;

namespace OES.DATA.Examinations;

public interface IExamRepository
{
    Task<IReadOnlyList<Exam>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Exam>> GetAvailableAsync(DateTime utcNow, CancellationToken cancellationToken = default);
    Task<Exam?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(ExamInput input, int createdBy, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, ExamInput input, CancellationToken cancellationToken = default);
    Task<bool> UpdateStatusAsync(int id, string status, bool isActive, CancellationToken cancellationToken = default);
}
