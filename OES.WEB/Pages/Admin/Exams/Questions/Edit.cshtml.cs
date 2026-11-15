using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OES.MODELS.Examinations;
using OES.SERVICES.Examinations;

namespace OES.WEB.Pages.Admin.Exams.Questions;

[Authorize(Policy = "AdminOnly")]
public sealed class EditModel(IQuestionService questionService, IExamService examService) : PageModel
{
    [BindProperty]
    public CreateModel.QuestionForm Input { get; set; } = new();

    public Exam? Exam { get; private set; }
    public int Id { get; private set; }

    public async Task<IActionResult> OnGetAsync(int examId, int id, CancellationToken cancellationToken)
    {
        Id = id;
        Exam = await examService.GetByIdAsync(examId, cancellationToken);
        var question = await questionService.GetByIdAsync(id, cancellationToken);
        if (Exam is null || question is null || question.ExamId != examId)
        {
            return NotFound();
        }

        Input = FromQuestion(question);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int examId, int id, CancellationToken cancellationToken)
    {
        Id = id;
        Exam = await examService.GetByIdAsync(examId, cancellationToken);
        if (Exam is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await questionService.UpdateAsync(id, Input.ToModel(), cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "The question could not be updated.");
            return Page();
        }

        return RedirectToPage("Index", new { examId });
    }

    private static CreateModel.QuestionForm FromQuestion(Question question)
    {
        var input = new CreateModel.QuestionForm
        {
            QuestionText = question.QuestionText,
            Marks = question.Marks,
            NegativeMarks = question.NegativeMarks,
            Options = question.Options.Select(option => new CreateModel.OptionForm
            {
                OptionText = option.OptionText,
                IsCorrect = option.IsCorrect
            }).ToList()
        };
        input.EnsureDefaultOptions();
        return input;
    }
}
