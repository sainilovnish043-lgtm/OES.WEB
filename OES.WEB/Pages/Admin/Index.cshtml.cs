using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OES.WEB.Pages.Admin;

[Authorize(Roles = "Admin")]
public sealed class IndexModel : PageModel
{
    public void OnGet()
    {
    }
}
