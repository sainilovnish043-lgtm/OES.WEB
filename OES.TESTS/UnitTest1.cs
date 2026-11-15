using OES.DATA.Examinations;
using OES.MODELS.Examinations;
using OES.SERVICES.Examinations;

namespace OES.TESTS;

public sealed class ExamServiceTests
{
    [Fact]
    public async Task CreateAsync_RejectsDurationLongerThanScheduledWindow()
    {
        var service = new ExamService(new FakeExamRepository());
        var input = new ExamInput
        {
            Title = "Assessment",
            StartUtc = DateTime.UtcNow,
            EndUtc = DateTime.UtcNow.AddMinutes(30),
            DurationMinutes = 60,
            TotalMarks = 10,
            PassingMarks = 5
        };

        var result = await service.CreateAsync(input, 1);

        Assert.False(result.Succeeded);
        Assert.Contains("Duration", result.Error);
    }
}

public sealed class QuestionServiceTests
{
    [Fact]
    public async Task CreateAsync_RequiresExactlyOneCorrectOption()
    {
        var service = new QuestionService(new FakeQuestionRepository(), new FakeExamRepository());
        var input = new QuestionInput
        {
            QuestionText = "Two plus two?",
            Marks = 1,
            Options =
            [
                new OptionInput { OptionText = "Three" },
                new OptionInput { OptionText = "Four" }
            ]
        };

        var result = await service.CreateAsync(1, input);

        Assert.False(result.Succeeded);
        Assert.Contains("Exactly one", result.Error);
    }

    [Fact]
    public async Task CreateAsync_RejectsPublishedExam()
    {
        var repository = new FakeExamRepository
        {
            Exam = new Exam { Id = 1, IsActive = true, Status = ExamStatuses.Published }
        };
        var service = new QuestionService(new FakeQuestionRepository(), repository);
        var input = new QuestionInput
        {
            QuestionText = "Question",
            Marks = 1,
            Options =
            [
                new OptionInput { OptionText = "A", IsCorrect = true },
                new OptionInput { OptionText = "B" }
            ]
        };

        var result = await service.CreateAsync(1, input);

        Assert.False(result.Succeeded);
        Assert.Contains("draft", result.Error, StringComparison.OrdinalIgnoreCase);
    }
}

internal sealed class FakeExamRepository : IExamRepository
{
    public Exam? Exam { get; init; } = new()
    {
        Id = 1,
        IsActive = true,
        Status = ExamStatuses.Draft,
        StartUtc = DateTime.UtcNow,
        EndUtc = DateTime.UtcNow.AddHours(1),
        TotalMarks = 10,
        PassingMarks = 5
    };

    public Task<IReadOnlyList<Exam>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Exam>>([]);
    public Task<IReadOnlyList<Exam>> GetAvailableAsync(DateTime utcNow, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Exam>>([]);
    public Task<Exam?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult(Exam);
    public Task<int> CreateAsync(ExamInput input, int createdBy, CancellationToken cancellationToken = default) => Task.FromResult(1);
    public Task<bool> UpdateAsync(int id, ExamInput input, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<bool> UpdateStatusAsync(int id, string status, bool isActive, CancellationToken cancellationToken = default) => Task.FromResult(true);
}

internal sealed class FakeQuestionRepository : IQuestionRepository
{
    public Task<IReadOnlyList<Question>> GetByExamIdAsync(int examId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Question>>([]);
    public Task<Question?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult<Question?>(null);
    public Task<int> CreateAsync(int examId, QuestionInput input, CancellationToken cancellationToken = default) => Task.FromResult(1);
    public Task<bool> UpdateAsync(int id, QuestionInput input, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult(true);
}
