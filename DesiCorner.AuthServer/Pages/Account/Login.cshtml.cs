using System.ComponentModel.DataAnnotations;
using DesiCorner.AuthServer.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DesiCorner.AuthServer.Pages.Account;

public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<LoginModel> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }

    public void OnGet()
    {
        // ReturnUrl is bound from query string
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            ErrorMessage = "Please enter a valid email and password.";
            return Page();
        }

        var user = await _userManager.FindByEmailAsync(Input.Email);
        if (user is null)
        {
            _logger.LogWarning("Login failed - user not found: {Email}", Input.Email);
            ErrorMessage = "Invalid email or password.";
            return Page();
        }

        // Check if email is confirmed
        if (!user.EmailConfirmed)
        {
            _logger.LogWarning("Login failed - email not confirmed: {Email}", Input.Email);
            ErrorMessage = "Please verify your email address before signing in. "
                + $"<a href=\"http://localhost:4200/auth/verify-otp?identifier={Uri.EscapeDataString(Input.Email)}&purpose=Registration\">Verify now</a>";
            return Page();
        }

        var result = await _signInManager.PasswordSignInAsync(
            user, Input.Password, isPersistent: false, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {Email} logged in via OAuth login page", Input.Email);

            // Update last login timestamp
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            {
                return LocalRedirect(ReturnUrl);
            }

            return LocalRedirect("/");
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("User {Email} is locked out", Input.Email);
            ErrorMessage = "Your account has been locked due to too many failed attempts. Please try again in 15 minutes.";
            return Page();
        }

        _logger.LogWarning("Login failed - invalid password for: {Email}", Input.Email);
        ErrorMessage = "Invalid email or password.";
        return Page();
    }
}
