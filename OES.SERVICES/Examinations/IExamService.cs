using OES.MODELS.Examinations;

namespace OES.SERVICES.Examinations;

public interface IExamService
{
    Task<IReadOnlyList<Exam>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Exam?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error, int? ExamId)> CreateAsync(ExamInput input, int createdBy, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error)> UpdateAsync(int id, ExamInput input, CancellationToken cancellationToken = default);
    Task<bool> PublishAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> UnpublishAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> DeactivateAsync(int id, CancellationToken cancellationToken = default);
}
