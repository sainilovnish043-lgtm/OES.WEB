using OES.DATA.Authentication;
using OES.DATA.Examinations;
using OES.MODELS.Examinations;

namespace OES.SERVICES.Examinations;

public sealed class ResultService(
    IResultRepository resultRepository,
    IAttemptRepository attemptRepository,
    IExamRepository examRepository,
    IQuestionRepository questionRepository,
    IUserRepository userRepository) : IResultService
{
    public async Task<Result?> GenerateAsync(int attemptId, CancellationToken cancellationToken = default)
    {
        var existing = await resultRepository.GetByAttemptIdAsync(attemptId, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var attempt = await attemptRepository.GetByIdAsync(attemptId, cancellationToken);
        if (attempt is null || attempt.Status == AttemptStatuses.InProgress)
        {
            return null;
        }

        var exam = await examRepository.GetByIdAsync(attempt.ExamId, cancellationToken);
        if (exam is null)
        {
            return null;
        }

        var questions = await questionRepository.GetByExamIdAsync(exam.Id, cancellationToken);
        var answers = (await attemptRepository.GetAnswersAsync(attempt.Id, cancellationToken))
            .ToDictionary(answer => answer.QuestionId);

        var correctAnswers = 0;
        var wrongAnswers = 0;
        var unansweredQuestions = 0;
        var obtainedMarks = 0m;

        foreach (var question in questions)
        {
            if (!answers.TryGetValue(question.Id, out var answer))
            {
                unansweredQuestions++;
                continue;
            }

            var correctOption = question.Options.FirstOrDefault(option => option.IsCorrect);
            if (correctOption?.Id == answer.SelectedOptionId)
            {
                correctAnswers++;
                obtainedMarks += question.Marks;
            }
            else
            {
                wrongAnswers++;
                obtainedMarks -= question.NegativeMarks;
            }
        }

        obtainedMarks = Math.Max(0m, obtainedMarks);
        var percentage = exam.TotalMarks <= 0
            ? 0m
            : Math.Round(obtainedMarks / exam.TotalMarks * 100m, 2);
        var result = new Result
        {
            AttemptId = attempt.Id,
            ExamId = exam.Id,
            StudentId = attempt.StudentId,
            TotalQuestions = questions.Count,
            CorrectAnswers = correctAnswers,
            WrongAnswers = wrongAnswers,
            UnansweredQuestions = unansweredQuestions,
            TotalMarks = exam.TotalMarks,
            ObtainedMarks = obtainedMarks,
            Percentage = percentage,
            PassingMarks = exam.PassingMarks,
            Status = obtainedMarks >= exam.PassingMarks ? ResultStatuses.Pass : ResultStatuses.Fail
        };

        await resultRepository.CreateAsync(result, cancellationToken);
        return await resultRepository.GetByAttemptIdAsync(attemptId, cancellationToken);
    }

    public async Task<IReadOnlyList<Result>> GetStudentResultsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var student = await userRepository.FindStudentByUserIdAsync(userId, cancellationToken);
        return student is null ? [] : await resultRepository.GetByStudentIdAsync(student.Id, cancellationToken);
    }

    public async Task<Result?> GetStudentResultAsync(int userId, int resultId, CancellationToken cancellationToken = default)
    {
        var student = await userRepository.FindStudentByUserIdAsync(userId, cancellationToken);
        return student is null
            ? null
            : await resultRepository.GetByIdForStudentAsync(resultId, student.Id, cancellationToken);
    }

    public Task<IReadOnlyList<Result>> GetAllAsync(int? examId = null, CancellationToken cancellationToken = default) =>
        resultRepository.GetAllAsync(examId, cancellationToken);
}
