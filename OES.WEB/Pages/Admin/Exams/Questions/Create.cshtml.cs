using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OES.MODELS.Examinations;
using OES.SERVICES.Examinations;

namespace OES.WEB.Pages.Admin.Exams.Questions;

[Authorize(Policy = "AdminOnly")]
public sealed class CreateModel(IQuestionService questionService, IExamService examService) : PageModel
{
    [BindProperty]
    public QuestionForm Input { get; set; } = new();

    public Exam? Exam { get; private set; }

    public async Task<IActionResult> OnGetAsync(int examId, CancellationToken cancellationToken)
    {
        Exam = await examService.GetByIdAsync(examId, cancellationToken);
        if (Exam is null)
        {
            return NotFound();
        }

        Input.EnsureDefaultOptions();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int examId, CancellationToken cancellationToken)
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

        var result = await questionService.CreateAsync(examId, Input.ToModel(), cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "The question could not be created.");
            return Page();
        }

        return RedirectToPage("Index", new { examId });
    }

    public sealed class QuestionForm
    {
        [Required, StringLength(2000)]
        [Display(Name = "Question text")]
        public string QuestionText { get; set; } = string.Empty;

        [Range(typeof(decimal), "0.01", "1000000")]
        public decimal Marks { get; set; } = 1;

        [Range(typeof(decimal), "0", "1000000")]
        [Display(Name = "Negative marks")]
        public decimal NegativeMarks { get; set; }

        [MinLength(2), MaxLength(6)]
        public List<OptionForm> Options { get; set; } = [];

        public void EnsureDefaultOptions()
        {
            if (Options.Count == 0)
            {
                Options =
                [
                    new OptionForm(),
                    new OptionForm(),
                    new OptionForm(),
                    new OptionForm()
                ];
            }
        }

        public QuestionInput ToModel() => new()
        {
            QuestionText = QuestionText,
            Marks = Marks,
            NegativeMarks = NegativeMarks,
            Options = Options.Select(option => new OptionInput
            {
                OptionText = option.OptionText,
                IsCorrect = option.IsCorrect
            }).ToList()
        };
    }

    public sealed class OptionForm
    {
        [Required, StringLength(1000)]
        [Display(Name = "Option text")]
        public string OptionText { get; set; } = string.Empty;

        public bool IsCorrect { get; set; }
    }
}
