using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OES.MODELS.Authentication;
using OES.SERVICES.Authentication;
using StudentModel = OES.MODELS.Authentication.Student;

namespace OES.WEB.Pages.Admin.Students;

[Authorize(Policy = "AdminOnly")]
public sealed class IndexModel(IAuthenticationService authenticationService) : PageModel
{
    public IReadOnlyList<StudentModel> Students { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Students = await authenticationService.GetStudentsAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostStatusAsync(
        int userId,
        bool isActive,
        CancellationToken cancellationToken)
    {
        await authenticationService.UpdateStudentStatusAsync(userId, isActive, cancellationToken);
        return RedirectToPage();
    }
}
