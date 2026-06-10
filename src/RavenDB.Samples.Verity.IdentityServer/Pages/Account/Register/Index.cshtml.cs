using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RavenDB.Samples.Verity.Model;
using System.ComponentModel.DataAnnotations;

namespace RavenDB.Samples.Verity.IdentityServer.Pages.Account.Register;

public class IndexModel(UserStore users, IConfiguration config) : PageModel
{
    private readonly string _bffBaseUrl =
        config["Bff:BaseUrl"] ?? throw new InvalidOperationException("Missing: Bff:BaseUrl");

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public async Task OnGetAsync(string? bffReturnUrl = null)
    {
        Input.BffReturnUrl = bffReturnUrl ?? "/";
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (Input.Password != Input.ConfirmPassword)
        {
            ModelState.AddModelError(nameof(Input.ConfirmPassword), "Passwords do not match.");
            return Page();
        }

        var (success, error) = await users.RegisterAsync(
            Input.Username, Input.Password, Input.DisplayName, Input.Email, Input.Role);

        if (!success)
        {
            ModelState.AddModelError(nameof(Input.Username), error);
            return Page();
        }

        var returnUrl = Uri.EscapeDataString(Input.BffReturnUrl ?? "/");
        return Redirect($"{_bffBaseUrl}/bff/login?returnUrl={returnUrl}");
    }

    public class InputModel
    {
        [Required, StringLength(50, MinimumLength = 2)]
        public string Username { get; set; } = string.Empty;

        [Required, StringLength(100, MinimumLength = 2)]
        public string DisplayName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(100, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string ConfirmPassword { get; set; } = string.Empty;

        public UserRole Role { get; set; } = UserRole.Viewer;

        public string BffReturnUrl { get; set; } = "/";
    }
}
