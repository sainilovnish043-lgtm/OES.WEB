using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OES.MODELS.Examinations;
using OES.SERVICES.Examinations;

namespace OES.WEB.Pages.Admin.Exams;

[Authorize(Policy = "AdminOnly")]
public sealed class CreateModel(IExamService examService) : PageModel
{
    [BindProperty]
    public ExamInputModel Input { get; set; } = new();

    public void OnGet()
    {
        var start = DateTime.Now.AddHours(1);
        Input.StartLocal = new DateTime(start.Year, start.Month, start.Day, start.Hour, 0, 0);
        Input.EndLocal = Input.StartLocal.AddHours(1);
        Input.DurationMinutes = 60;
        Input.TotalMarks = 100;
        Input.PassingMarks = 40;
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var createdBy))
        {
            return Forbid();
        }

        var result = await examService.CreateAsync(ToInput(), createdBy, cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "The exam could not be created.");
            return Page();
        }

        return RedirectToPage("Details", new { id = result.ExamId });
    }

    private ExamInput ToInput() => new()
    {
        Title = Input.Title,
        Description = Input.Description,
        StartUtc = ToUtc(Input.StartLocal),
        EndUtc = ToUtc(Input.EndLocal),
        DurationMinutes = Input.DurationMinutes,
        TotalMarks = Input.TotalMarks,
        PassingMarks = Input.PassingMarks
    };

    internal static DateTime ToUtc(DateTime local) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified));

    public sealed class ExamInputModel
    {
        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Description { get; set; }

        [Display(Name = "Start time")]
        [DataType(DataType.DateTime)]
        public DateTime StartLocal { get; set; }

        [Display(Name = "End time")]
        [DataType(DataType.DateTime)]
        public DateTime EndLocal { get; set; }

        [Range(1, 1440)]
        [Display(Name = "Duration (minutes)")]
        public int DurationMinutes { get; set; }

        [Range(typeof(decimal), "0.01", "1000000")]
        [Display(Name = "Total marks")]
        public decimal TotalMarks { get; set; }

        [Range(typeof(decimal), "0", "1000000")]
        [Display(Name = "Passing marks")]
        public decimal PassingMarks { get; set; }
    }
}
