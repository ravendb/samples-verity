using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using RavenDB.Samples.Verity.Model;
using System.ComponentModel.DataAnnotations;

namespace RavenDB.Samples.Verity.IdentityServer.Pages.Account.Register;

public class IndexModel(UserStore users, IConfiguration config, IDocumentStore store) : PageModel
{
    private readonly string _bffBaseUrl =
        config["Bff:BaseUrl"] ?? throw new InvalidOperationException("Missing: Bff:BaseUrl");

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<SelectListItem> CompanyOptions { get; private set; } = [];

    public async Task OnGetAsync(string? bffReturnUrl = null)
    {
        Input.BffReturnUrl = bffReturnUrl ?? "/";
        await LoadCompaniesAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadCompaniesAsync();
            return Page();
        }

        if (Input.Password != Input.ConfirmPassword)
        {
            ModelState.AddModelError(nameof(Input.ConfirmPassword), "Passwords do not match.");
            await LoadCompaniesAsync();
            return Page();
        }

        if (Input.Role == UserRole.Employee && string.IsNullOrWhiteSpace(Input.CompanyId))
        {
            ModelState.AddModelError(nameof(Input.CompanyId), "Please select a company.");
            await LoadCompaniesAsync();
            return Page();
        }

        var (success, error) = await users.RegisterAsync(
            Input.Username,
            Input.Password,
            Input.DisplayName,
            Input.Email,
            Input.Role,
            Input.Role == UserRole.Employee ? Input.CompanyId?.Trim() : null);

        if (!success)
        {
            ModelState.AddModelError(nameof(Input.Username), error);
            await LoadCompaniesAsync();
            return Page();
        }

        var returnUrl = Uri.EscapeDataString(Input.BffReturnUrl ?? "/");
        return Redirect($"{_bffBaseUrl}/bff/login?returnUrl={returnUrl}");
    }

    private async Task LoadCompaniesAsync()
    {
        using var session = store.OpenAsyncSession();
        var companies = await session.Query<Company>()
            .OrderBy(c => c.Name)
            .Take(200)
            .ToListAsync();

        CompanyOptions = companies
            .Select(c => new SelectListItem(c.Name, c.Id))
            .ToList();
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

        public UserRole Role { get; set; } = UserRole.User;

        public string? CompanyId { get; set; }

        public string BffReturnUrl { get; set; } = "/";
    }
}
