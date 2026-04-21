using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RavenDB.Samples.Verity.Identity.Pages;

[AllowAnonymous]
public class IndexModel : PageModel
{
    public void OnGet() { }
}
