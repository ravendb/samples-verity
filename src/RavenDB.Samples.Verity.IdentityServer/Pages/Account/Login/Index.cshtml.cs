using Duende.IdentityServer;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace RavenDB.Samples.Verity.IdentityServer.Pages.Account.Login;

public class IndexModel(
    UserStore users,
    IIdentityServerInteractionService interaction,
    IConfiguration configuration) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool Registered { get; set; }
    public string? ErrorMessage { get; set; }

    public void OnGet(string? returnUrl = null, bool registered = false)
    {
        Input.ReturnUrl = returnUrl;
        Registered = registered;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            var context = await interaction.GetAuthorizationContextAsync(Input.ReturnUrl);

            if (await users.ValidateCredentialsAsync(Input.Username, Input.Password))
            {
                var user = (await users.FindByUsernameAsync(Input.Username))!;

                await HttpContext.SignInAsync(
                    new IdentityServerUser(user.SubjectId)
                    {
                        AdditionalClaims = users.GetClaims(user).ToList(),
                    },
                    new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc   = DateTimeOffset.UtcNow.Add(TimeSpan.FromDays(1)),
                    });

                if (context != null)
                    return Redirect(Input.ReturnUrl!);

                if (Url.IsLocalUrl(Input.ReturnUrl))
                    return LocalRedirect(Input.ReturnUrl);

                var bffBaseUrl = configuration["Bff:BaseUrl"] ?? "/";
                return Redirect(bffBaseUrl);
            }

            ErrorMessage = "Invalid username or password.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Login error: {ex.Message}";
        }

        Input.Password = string.Empty;
        return Page();
    }

    public class InputModel
    {
        public string  Username  { get; set; } = string.Empty;
        public string  Password  { get; set; } = string.Empty;
        public string? ReturnUrl { get; set; }
    }
}
