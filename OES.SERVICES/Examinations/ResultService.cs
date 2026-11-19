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
    {
        var existing = await resultRepository.GetByAttemptIdAsync(attemptId, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var attempt = await attemptRepository.GetByIdAsync(attemptId, cancellationToken);
        {
            return null;
        }

        var exam = await examRepository.GetByIdAsync(attempt.ExamId, cancellationToken);
        if (exam is null)
        {
            return null;
        }

        var questions = await questionRepository.GetByExamIdAsync(exam.Id, cancellationToken);


        foreach (var question in questions)
        {
            {
                {
                correctAnswers++;
                    obtainedMarks += question.Marks;
                    correctCount++;
                }
                else
                {
                wrongAnswers++;
                    obtainedMarks -= question.NegativeMarks;
                    wrongCount++;
                }
            }
            else
            {
                unattemptedCount++;
            }
        }

        {
            ExamId = exam.Id,
            StudentId = attempt.StudentId,
            ObtainedMarks = obtainedMarks,
            Percentage = percentage,
        };

    }

    {
        var student = await userRepository.FindStudentByUserIdAsync(userId, cancellationToken);
    }

    {
        var student = await userRepository.FindStudentByUserIdAsync(userId, cancellationToken);
    }

}
