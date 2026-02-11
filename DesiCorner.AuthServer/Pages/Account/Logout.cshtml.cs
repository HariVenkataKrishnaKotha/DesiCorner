using DesiCorner.AuthServer.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DesiCorner.AuthServer.Pages.Account;

public class LogoutModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<LogoutModel> _logger;

    private static readonly HashSet<string> AllowedOrigins = new(StringComparer.OrdinalIgnoreCase)
    {
        "http://localhost:4200",
        "https://localhost:4200"
    };

    public LogoutModel(
        SignInManager<ApplicationUser> signInManager,
        ILogger<LogoutModel> logger)
    {
        _signInManager = signInManager;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(string? post_logout_redirect_uri)
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User signed out via OAuth logout endpoint");

        // Validate redirect URI against allowed origins to prevent open redirect
        if (!string.IsNullOrEmpty(post_logout_redirect_uri)
            && Uri.TryCreate(post_logout_redirect_uri, UriKind.Absolute, out var uri)
            && AllowedOrigins.Any(o => post_logout_redirect_uri.StartsWith(o, StringComparison.OrdinalIgnoreCase)))
        {
            return Redirect(post_logout_redirect_uri);
        }

        return Redirect("/");
    }
}
