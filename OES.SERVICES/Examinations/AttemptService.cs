using OES.DATA.Authentication;
using OES.DATA.Examinations;
using OES.MODELS.Examinations;

namespace OES.SERVICES.Examinations;

public sealed class AttemptService(
    IAttemptRepository attemptRepository,
    IExamRepository examRepository,
    IQuestionRepository questionRepository,
    IUserRepository userRepository,
    IResultService resultService) : IAttemptService
{
    public async Task<IReadOnlyList<Exam>> GetAvailableExamsAsync(
        int userId,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var student = await userRepository.FindStudentByUserIdAsync(userId, cancellationToken);
        return student is { IsActive: true }
            ? await examRepository.GetAvailableAsync(utcNow, cancellationToken)
            : [];
    }

    public async Task<(bool Succeeded, string? Error, int? AttemptId)> StartAsync(
        int userId,
        int examId,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var student = await userRepository.FindStudentByUserIdAsync(userId, cancellationToken);
        if (student is null || !student.IsActive)
        {
            return (false, "Active student profile was not found.", null);
        }

        var exam = await examRepository.GetByIdAsync(examId, cancellationToken);
        if (exam is null || !exam.IsActive || exam.Status != ExamStatuses.Published)
        {
            return (false, "This exam is not available.", null);
        }

        if (utcNow < exam.StartUtc || utcNow >= exam.EndUtc)
        {
            return (false, "This exam is outside its scheduled time.", null);
        }

        var existing = await attemptRepository.GetForExamAsync(examId, student.Id, cancellationToken);
        if (existing is not null)
        {
            return existing.Status == AttemptStatuses.InProgress
                ? (true, null, existing.Id)
                : (false, "You have already submitted this exam.", null);
        }

        var expiresUtc = new[] { utcNow.AddMinutes(exam.DurationMinutes), exam.EndUtc }.Min();
        var attemptId = await attemptRepository.CreateAsync(examId, student.Id, utcNow, expiresUtc, cancellationToken);
        return (true, null, attemptId);
    }

    public async Task<AttemptSession?> GetSessionAsync(
        int userId,
        int attemptId,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var student = await userRepository.FindStudentByUserIdAsync(userId, cancellationToken);
        if (student is null || !student.IsActive)
        {
            return null;
        }

        var attempt = await attemptRepository.GetByIdForStudentAsync(attemptId, student.Id, cancellationToken);
        if (attempt is null)
        {
            return null;
        }

        var exam = await examRepository.GetByIdAsync(attempt.ExamId, cancellationToken);
        if (exam is null)
        {
            return null;
        }

        if (attempt.Status == AttemptStatuses.InProgress && utcNow >= attempt.ExpiresUtc)
        {
            await attemptRepository.MarkTimedOutAsync(attempt.Id, utcNow, cancellationToken);
            await resultService.CalculateAndSaveResultAsync(attempt.Id, cancellationToken);
            attempt = new ExamAttempt
            {
                Id = attempt.Id,
                ExamId = attempt.ExamId,
                StudentId = attempt.StudentId,
                StartedUtc = attempt.StartedUtc,
                ExpiresUtc = attempt.ExpiresUtc,
                SubmittedUtc = utcNow,
                Status = AttemptStatuses.TimedOut
            };
        }

        var questions = await questionRepository.GetByExamIdAsync(exam.Id, cancellationToken);
        var answers = await attemptRepository.GetAnswersAsync(attempt.Id, cancellationToken);
        return new AttemptSession
        {
            Attempt = attempt,
            Exam = exam,
            Questions = questions,
            Answers = answers
        };
    }

    public async Task<(bool Succeeded, string? Error)> SubmitAsync(
        int userId,
        int attemptId,
        IReadOnlyList<AnswerInput> answers,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var student = await userRepository.FindStudentByUserIdAsync(userId, cancellationToken);
        if (student is null || !student.IsActive)
        {
            return (false, "Active student profile was not found.");
        }

        var attempt = await attemptRepository.GetByIdForStudentAsync(attemptId, student.Id, cancellationToken);
        if (attempt is null)
        {
            return (false, "Exam attempt was not found.");
        }

        if (attempt.Status != AttemptStatuses.InProgress)
        {
            return (false, "This exam attempt is already closed.");
        }

        var questions = await questionRepository.GetByExamIdAsync(attempt.ExamId, cancellationToken);
        var validQuestions = questions.ToDictionary(question => question.Id);
        var normalizedAnswers = answers
            .Where(answer => answer.SelectedOptionId.HasValue)
            .GroupBy(answer => answer.QuestionId)
            .Select(group => group.Last())
            .ToList();

        foreach (var answer in normalizedAnswers)
        {
            if (!validQuestions.TryGetValue(answer.QuestionId, out var question) ||
                question.Options.All(option => option.Id != answer.SelectedOptionId!.Value))
            {
                return (false, "One or more submitted answers are invalid.");
            }
        }

        var status = utcNow >= attempt.ExpiresUtc ? AttemptStatuses.TimedOut : AttemptStatuses.Submitted;
        var saved = await attemptRepository.SubmitAsync(attemptId, normalizedAnswers, status, utcNow, cancellationToken);
        if (!saved)
        {
            return (false, "The exam attempt could not be submitted.");
        }

        await resultService.CalculateAndSaveResultAsync(attemptId, cancellationToken);

        return status == AttemptStatuses.TimedOut
            ? (false, "The exam time expired. Your answers were submitted automatically.")
            : (true, null);
    }
}
