using OES.DATA.Examinations;
using OES.MODELS.Examinations;

namespace OES.SERVICES.Examinations;

public sealed class ExamService(IExamRepository examRepository) : IExamService
{
    public Task<IReadOnlyList<Exam>> GetAllAsync(CancellationToken cancellationToken = default) =>
        examRepository.GetAllAsync(cancellationToken);

    public Task<Exam?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        examRepository.GetByIdAsync(id, cancellationToken);

    public async Task<(bool Succeeded, string? Error, int? ExamId)> CreateAsync(
        ExamInput input,
        int createdBy,
        CancellationToken cancellationToken = default)
    {
        var validationError = Validate(input);
        if (validationError is not null)
        {
            return (false, validationError, null);
        }

        var examId = await examRepository.CreateAsync(input, createdBy, cancellationToken);
        return (true, null, examId);
    }

    public async Task<(bool Succeeded, string? Error)> UpdateAsync(
        int id,
        ExamInput input,
        CancellationToken cancellationToken = default)
    {
        var validationError = Validate(input);
        if (validationError is not null)
        {
            return (false, validationError);
        }

        var updated = await examRepository.UpdateAsync(id, input, cancellationToken);
        return updated
            ? (true, null)
            : (false, "Only active draft exams can be edited.");
    }

    public async Task<bool> PublishAsync(int id, CancellationToken cancellationToken = default)
    {
        var exam = await examRepository.GetByIdAsync(id, cancellationToken);
        if (exam is null || !exam.IsActive || exam.Status != ExamStatuses.Draft)
        {
            return false;
        }

        return await examRepository.UpdateStatusAsync(id, ExamStatuses.Published, true, cancellationToken);
    }

    public async Task<bool> UnpublishAsync(int id, CancellationToken cancellationToken = default)
    {
        var exam = await examRepository.GetByIdAsync(id, cancellationToken);
        if (exam is null || !exam.IsActive || exam.Status != ExamStatuses.Published)
        {
            return false;
        }

        return await examRepository.UpdateStatusAsync(id, ExamStatuses.Draft, true, cancellationToken);
    }

    public async Task<bool> DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var exam = await examRepository.GetByIdAsync(id, cancellationToken);
        if (exam is null || !exam.IsActive)
        {
            return false;
        }

        return await examRepository.UpdateStatusAsync(id, ExamStatuses.Cancelled, false, cancellationToken);
    }

    private static string? Validate(ExamInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Title))
        {
            return "Exam title is required.";
        }

        if (input.StartUtc >= input.EndUtc)
        {
            return "Exam end time must be later than the start time.";
        }

        if (input.DurationMinutes <= 0)
        {
            return "Duration must be greater than zero minutes.";
        }

        if (input.DurationMinutes > (input.EndUtc - input.StartUtc).TotalMinutes)
        {
            return "Duration cannot be longer than the scheduled exam window.";
        }

        if (input.TotalMarks <= 0)
        {
            return "Total marks must be greater than zero.";
        }

        if (input.PassingMarks < 0 || input.PassingMarks > input.TotalMarks)
        {
            return "Passing marks must be between zero and total marks.";
        }

        return null;
    }
}
