using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RavenDB.Samples.Verity.IdentityServer.Pages.Account.Logout;

public class IndexModel(IIdentityServerInteractionService interaction) : PageModel
{
    public async Task<IActionResult> OnGetAsync(string? logoutId = null)
    {
        var context = await interaction.GetLogoutContextAsync(logoutId);
        await HttpContext.SignOutAsync();

        var redirectUri = context?.PostLogoutRedirectUri;
        return redirectUri is not null
            ? Redirect(redirectUri)
            : Redirect("~/");
    }

    public async Task<IActionResult> OnPostAsync(string? logoutId = null) =>
        await OnGetAsync(logoutId);
}
