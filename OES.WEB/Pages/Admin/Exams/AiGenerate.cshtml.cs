using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OES.MODELS.Ai;
using OES.MODELS.Examinations;
using OES.SERVICES.Ai;
using OES.SERVICES.Examinations;

namespace OES.WEB.Pages.Admin.Exams;

[Authorize(Policy = "AdminOnly")]
public sealed class AiGenerateModel(
    IExamService examService,
    IQuestionService questionService,
    IAiService aiService) : PageModel
{
    public Exam? Exam { get; private set; }

    [BindProperty]
    public GenerateForm Input { get; set; } = new();

    public IReadOnlyList<AiGeneratedQuestion> GeneratedQuestions { get; private set; } = [];

    [BindProperty]
    public string QuestionsJson { get; set; } = "[]";

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public sealed class GenerateForm
    {
        [Required(ErrorMessage = "Please enter a topic for AI generation.")]
        [StringLength(200)]
        public string Topic { get; set; } = string.Empty;

        [Required]
        public string Difficulty { get; set; } = "Medium";

        [Range(1, 15, ErrorMessage = "Count must be between 1 and 15 questions.")]
        public int Count { get; set; } = 5;

        [Range(0.5, 100, ErrorMessage = "Marks must be at least 0.5.")]
        public decimal Marks { get; set; } = 1;

        [Range(0, 50, ErrorMessage = "Negative marks cannot be negative.")]
        public decimal NegativeMarks { get; set; } = 0;
    }

    public async Task<IActionResult> OnGetAsync(int examId, CancellationToken cancellationToken)
    {
        Exam = await examService.GetByIdAsync(examId, cancellationToken);
        if (Exam is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(Input.Topic))
        {
            Input.Topic = Exam.Title;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostGenerateAsync(int examId, CancellationToken cancellationToken)
    {
        Exam = await examService.GetByIdAsync(examId, cancellationToken);
        if (Exam is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var questions = await aiService.GenerateQuestionsAsync(new AiGenerateQuestionsRequest
        {
            Topic = Input.Topic,
            Difficulty = Input.Difficulty,
            Count = Input.Count,
            Marks = Input.Marks,
            NegativeMarks = Input.NegativeMarks
        }, cancellationToken);

        GeneratedQuestions = questions;
        QuestionsJson = JsonSerializer.Serialize(questions);

        if (questions.Count == 0)
        {
            ErrorMessage = "Could not generate questions. Please try again with a different topic.";
        }
        else
        {
            StatusMessage = $"✨ Successfully generated {questions.Count} questions! Review below and click 'Save to Exam'.";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync(int examId, CancellationToken cancellationToken)
    {
        Exam = await examService.GetByIdAsync(examId, cancellationToken);
        if (Exam is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(QuestionsJson) || QuestionsJson == "[]")
        {
            ErrorMessage = "No questions to save. Please generate questions first.";
            return Page();
        }

        var questions = JsonSerializer.Deserialize<List<AiGeneratedQuestion>>(QuestionsJson);
        if (questions is null || questions.Count == 0)
        {
            ErrorMessage = "Failed to parse questions data.";
            return Page();
        }

        int savedCount = 0;
        foreach (var q in questions)
        {
            var options = q.Options.Select(o => new OptionInput
            {
                OptionText = o.OptionText,
                IsCorrect = o.IsCorrect
            }).ToList();

            var input = new QuestionInput
            {
                QuestionText = q.QuestionText,
                Marks = Input.Marks > 0 ? Input.Marks : 1,
                NegativeMarks = Input.NegativeMarks,
                Options = options
            };

            var result = await questionService.CreateAsync(examId, input, cancellationToken);
            if (result.Succeeded)
            {
                savedCount++;
            }
        }

        StatusMessage = $"✨ {savedCount} AI-generated questions were added to {Exam.Title}!";
        return RedirectToPage("/Admin/Exams/Questions/Index", new { examId });
    }
}
