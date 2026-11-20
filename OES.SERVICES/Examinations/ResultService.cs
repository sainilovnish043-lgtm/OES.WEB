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
    public async Task<ExamResult?> CalculateAndSaveResultAsync(int attemptId, CancellationToken cancellationToken = default)
    {
        var existing = await resultRepository.GetByAttemptIdAsync(attemptId, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var attempt = await attemptRepository.GetByIdAsync(attemptId, cancellationToken);
        if (attempt is null)
        {
            return null;
        }

        var exam = await examRepository.GetByIdAsync(attempt.ExamId, cancellationToken);
        if (exam is null)
        {
            return null;
        }

        var questions = await questionRepository.GetByExamIdAsync(exam.Id, cancellationToken);
        var answers = await attemptRepository.GetAnswersAsync(attemptId, cancellationToken);
        var answerMap = answers.ToDictionary(a => a.QuestionId, a => a.SelectedOptionId);

        decimal obtainedMarks = 0;
        int correctCount = 0;
        int wrongCount = 0;
        int unattemptedCount = 0;

        foreach (var question in questions)
        {
            if (answerMap.TryGetValue(question.Id, out var selectedOptionId) && selectedOptionId > 0)
            {
                var correctOption = question.Options.FirstOrDefault(o => o.IsCorrect);
                if (correctOption is not null && correctOption.Id == selectedOptionId)
                {
                    obtainedMarks += question.Marks;
                    correctCount++;
                }
                else
                {
                    obtainedMarks -= question.NegativeMarks;
                    wrongCount++;
                }
            }
            else
            {
                unattemptedCount++;
            }
        }

        if (obtainedMarks < 0)
        {
            obtainedMarks = 0;
        }

        var totalMarks = exam.TotalMarks > 0 ? exam.TotalMarks : questions.Sum(q => q.Marks);
        var percentage = totalMarks > 0 ? Math.Round((obtainedMarks / totalMarks) * 100, 2) : 0;
        var isPassed = obtainedMarks >= exam.PassingMarks;

        var result = new ExamResult
        {
            AttemptId = attemptId,
            ExamId = exam.Id,
            StudentId = attempt.StudentId,
            TotalMarks = totalMarks,
            PassingMarks = exam.PassingMarks,
            ObtainedMarks = obtainedMarks,
            Percentage = percentage,
            CorrectAnswers = correctCount,
            WrongAnswers = wrongCount,
            UnattemptedQuestions = unattemptedCount,
            IsPassed = isPassed,
            CalculatedUtc = DateTime.UtcNow
        };

        var id = await resultRepository.SaveResultAsync(result, cancellationToken);
        return new ExamResult
        {
            Id = id,
            AttemptId = result.AttemptId,
            ExamId = result.ExamId,
            StudentId = result.StudentId,
            TotalMarks = result.TotalMarks,
            PassingMarks = result.PassingMarks,
            ObtainedMarks = result.ObtainedMarks,
            Percentage = result.Percentage,
            CorrectAnswers = result.CorrectAnswers,
            WrongAnswers = result.WrongAnswers,
            UnattemptedQuestions = result.UnattemptedQuestions,
            IsPassed = result.IsPassed,
            CalculatedUtc = result.CalculatedUtc
        };
    }

    public async Task<ExamResult?> GetResultByAttemptAsync(int attemptId, int userId, CancellationToken cancellationToken = default)
    {
        var student = await userRepository.FindStudentByUserIdAsync(userId, cancellationToken);
        var attempt = await attemptRepository.GetByIdAsync(attemptId, cancellationToken);
        if (attempt is null)
        {
            return null;
        }

        // If student, ensure they own the attempt
        if (student is not null && attempt.StudentId != student.Id)
        {
            return null;
        }

        var result = await resultRepository.GetByAttemptIdAsync(attemptId, cancellationToken);
        if (result is null && attempt.Status != AttemptStatuses.InProgress)
        {
            // Calculate if not yet calculated
            result = await CalculateAndSaveResultAsync(attemptId, cancellationToken);
        }

        return result;
    }

    public Task<IReadOnlyList<ExamWiseResultItem>> GetExamResultsAsync(int examId, CancellationToken cancellationToken = default) =>
        resultRepository.GetByExamIdAsync(examId, cancellationToken);

    public async Task<IReadOnlyList<StudentWiseResultItem>> GetStudentResultsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var student = await userRepository.FindStudentByUserIdAsync(userId, cancellationToken);
        if (student is null)
        {
            return [];
        }

        return await resultRepository.GetByStudentIdAsync(student.Id, cancellationToken);
    }

    public Task<IReadOnlyList<ExamWiseResultItem>> GetAllResultsAsync(CancellationToken cancellationToken = default) =>
        resultRepository.GetAllResultsAsync(cancellationToken);

    public Task<ResultSummary> GetExamSummaryAsync(int examId, CancellationToken cancellationToken = default) =>
        resultRepository.GetExamSummaryAsync(examId, cancellationToken);
}
