using Duende.IdentityModel;
using Duende.IdentityServer;
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RavenDB.Samples.Verity.Identity.Stores;
using System.Security.Claims;

namespace RavenDB.Samples.Verity.Identity.Pages.Account;

[AllowAnonymous]
public class LoginModel(
    RavenDbUserStore users,
    IIdentityServerInteractionService interaction,
    IEventService events) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    [BindProperty]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        var context = ReturnUrl is null ? null : await interaction.GetAuthorizationContextAsync(ReturnUrl);

        var (ok, user) = await users.ValidateAsync(Username, Password);
        if (!ok || user is null)
        {
            await events.RaiseAsync(new UserLoginFailureEvent(Username, "invalid credentials", clientId: context?.Client.ClientId));
            ErrorMessage = "Invalid username or password.";
            return Page();
        }

        await events.RaiseAsync(new UserLoginSuccessEvent(user.Username, user.SubjectId, user.DisplayName, clientId: context?.Client.ClientId));

        var claims = new List<Claim>
        {
            new(JwtClaimTypes.Name, user.DisplayName),
            new(JwtClaimTypes.Email, user.Email),
            new(JwtClaimTypes.PreferredUserName, user.Username)
        };
        foreach (var c in user.Claims)
            claims.Add(new Claim(c.Type, c.Value));

        await HttpContext.SignInAsync(new IdentityServerUser(user.SubjectId)
        {
            DisplayName = user.DisplayName,
            AdditionalClaims = claims.ToArray()
        }, new AuthenticationProperties
        {
            IsPersistent = false,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        });

        if (interaction.IsValidReturnUrl(ReturnUrl) || Url.IsLocalUrl(ReturnUrl))
            return Redirect(ReturnUrl!);

        return Redirect("~/");
    }
}
