using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OES.MODELS.Examinations;
using OES.SERVICES.Examinations;

namespace OES.WEB.Pages.Admin.Exams;

[Authorize(Policy = "AdminOnly")]
public sealed class EditModel(IExamService examService) : PageModel
{
    [BindProperty]
    public CreateModel.ExamInputModel Input { get; set; } = new();

    public int Id { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var exam = await examService.GetByIdAsync(id, cancellationToken);
        if (exam is null)
        {
            return NotFound();
        }

        Id = id;
        Input = FromExam(exam);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id, CancellationToken cancellationToken)
    {
        Id = id;
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await examService.UpdateAsync(id, ToInput(), cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "The exam could not be updated.");
            return Page();
        }

        return RedirectToPage("Details", new { id });
    }

    private ExamInput ToInput() => new()
    {
        Title = Input.Title,
        Description = Input.Description,
        StartUtc = CreateModel.ToUtc(Input.StartLocal),
        EndUtc = CreateModel.ToUtc(Input.EndLocal),
        DurationMinutes = Input.DurationMinutes,
        TotalMarks = Input.TotalMarks,
        PassingMarks = Input.PassingMarks
    };

    private static CreateModel.ExamInputModel FromExam(Exam exam) => new()
    {
        Title = exam.Title,
        Description = exam.Description,
        StartLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(exam.StartUtc, DateTimeKind.Utc), TimeZoneInfo.Local),
        EndLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(exam.EndUtc, DateTimeKind.Utc), TimeZoneInfo.Local),
        DurationMinutes = exam.DurationMinutes,
        TotalMarks = exam.TotalMarks,
        PassingMarks = exam.PassingMarks
    };
}
