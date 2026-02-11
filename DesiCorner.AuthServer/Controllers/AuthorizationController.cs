using DesiCorner.AuthServer.Identity;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace DesiCorner.AuthServer.Controllers;

[ApiController]
public class AuthorizationController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<AuthorizationController> _logger;

    public AuthorizationController(
        UserManager<ApplicationUser> users,
        SignInManager<ApplicationUser> signInManager,
        ILogger<AuthorizationController> logger)
    {
        _users = users;
        _signInManager = signInManager;
        _logger = logger;
    }

    [HttpGet("/connect/authorize")]
    [HttpPost("/connect/authorize")]
    [Authorize(AuthenticationSchemes = "Identity.Application")]
    public async Task<IActionResult> AuthorizeAsync()
    {
        var request = HttpContext.GetOpenIddictServerRequest();

        if (request is null)
        {
            _logger.LogError("OpenIddict request is null");
            return BadRequest(new
            {
                error = "invalid_request",
                error_description = "The OpenID Connect request cannot be retrieved."
            });
        }

        _logger.LogInformation("Authorization request - ClientId: {ClientId}, ResponseType: {ResponseType}",
            request.ClientId, request.ResponseType);

        if (string.IsNullOrEmpty(request.ClientId))
        {
            return BadRequest(new
            {
                error = "invalid_request",
                error_description = "The mandatory 'client_id' parameter is missing."
            });
        }

        var user = await _users.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var roles = await _users.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(Claims.Subject, user.Id.ToString()),
            new(Claims.Name, user.UserName ?? user.Email ?? user.Id.ToString()),
            new(Claims.Email, user.Email ?? ""),
            new(Claims.PhoneNumber, user.PhoneNumber ?? "")
        };

        foreach (var r in roles)
            claims.Add(new Claim(Claims.Role, r));

        // Add custom claims
        if (user.DietaryPreference is not null)
            claims.Add(new Claim("dietary_preference", user.DietaryPreference));

        claims.Add(new Claim("reward_points", user.RewardPoints.ToString()));

        // Admin permission
        if (roles.Contains("Admin"))
            claims.Add(new Claim("permission", "admin"));

        var identity = new ClaimsIdentity(claims, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var allowed = new HashSet<string>
        {
            Scopes.OpenId,
            Scopes.Profile,
            Scopes.Email,
            Scopes.Phone,
            Scopes.OfflineAccess,
            "desicorner.products.read",
            "desicorner.products.write",
            "desicorner.cart",
            "desicorner.orders.read",
            "desicorner.orders.write",
            "desicorner.payment",
            "desicorner.admin"
        };

        principal.SetScopes(request.GetScopes().Where(allowed.Contains));
        principal.SetResources("desicorner-api");

        principal.SetDestinations(static claim => claim.Type switch
        {
            Claims.Name or Claims.Subject or Claims.Role or Claims.Email or Claims.PhoneNumber =>
                new[] { Destinations.AccessToken, Destinations.IdentityToken },
            "dietary_preference" or "reward_points" or "permission" =>
                new[] { Destinations.AccessToken },
            _ =>
                new[] { Destinations.AccessToken }
        });

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost("/connect/token")]
    [Produces("application/json")]
    public async Task<IActionResult> TokenAsync()
    {
        var request = HttpContext.GetOpenIddictServerRequest();

        if (request is null)
        {
            return BadRequest(new
            {
                error = "invalid_request",
                error_description = "The OpenID Connect request cannot be retrieved."
            });
        }

        _logger.LogInformation("Token request - GrantType: {GrantType}, ClientId: {ClientId}",
            request.GrantType, request.ClientId);

        ClaimsPrincipal principal;

        // PASSWORD GRANT - NEW
        if (request.IsPasswordGrantType())
        {
            _logger.LogInformation("Processing password grant for user: {Username}", request.Username);

            // Find user by email (username in this case)
            var user = await _users.FindByEmailAsync(request.Username ?? string.Empty);
            if (user is null)
            {
                _logger.LogWarning("Password grant failed - user not found: {Username}", request.Username);
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Invalid username or password."
                    }));
            }

            // Verify password
            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password ?? string.Empty, lockoutOnFailure: true);

            if (!result.Succeeded)
            {
                _logger.LogWarning("Password grant failed - invalid password for user: {Username}", request.Username);

                if (result.IsLockedOut)
                {
                    return Forbid(
                        authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                        properties: new AuthenticationProperties(new Dictionary<string, string?>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Account is locked."
                        }));
                }

                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Invalid username or password."
                    }));
            }

            // Check if email is verified
            if (!user.EmailConfirmed)
            {
                _logger.LogWarning("Password grant failed - email not confirmed for user: {Username}", request.Username);
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Please verify your email address before logging in. Check your inbox for the verification code."
                    }));
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _users.UpdateAsync(user);

            // Get user roles
            var roles = await _users.GetRolesAsync(user);

            // Create claims
            var claims = new List<Claim>
            {
                new(Claims.Subject, user.Id.ToString()),
                new(Claims.Name, user.UserName ?? user.Email ?? user.Id.ToString()),
                new(Claims.Email, user.Email ?? ""),
                new(Claims.PhoneNumber, user.PhoneNumber ?? "")
            };

            foreach (var role in roles)
                claims.Add(new Claim(Claims.Role, role));

            // Add custom claims
            if (user.DietaryPreference is not null)
                claims.Add(new Claim("dietary_preference", user.DietaryPreference));

            claims.Add(new Claim("reward_points", user.RewardPoints.ToString()));

            // Admin permission
            if (roles.Contains("Admin"))
                claims.Add(new Claim("permission", "admin"));

            var identity = new ClaimsIdentity(claims, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            principal = new ClaimsPrincipal(identity);

            // Set scopes based on request
            var allowedScopes = new HashSet<string>
            {
                Scopes.OpenId,
                Scopes.Profile,
                Scopes.Email,
                Scopes.Phone,
                Scopes.OfflineAccess,
                "desicorner.products.read",
                "desicorner.products.write",
                "desicorner.cart",
                "desicorner.orders.read",
                "desicorner.orders.write",
                "desicorner.payment",
                "desicorner.admin"
            };

            principal.SetScopes(request.GetScopes().Where(allowedScopes.Contains));
            principal.SetResources("desicorner-api");

            principal.SetDestinations(static claim => claim.Type switch
            {
                Claims.Name or Claims.Subject or Claims.Role or Claims.Email or Claims.PhoneNumber =>
                    new[] { Destinations.AccessToken, Destinations.IdentityToken },
                "dietary_preference" or "reward_points" or "permission" =>
                    new[] { Destinations.AccessToken },
                _ =>
                    new[] { Destinations.AccessToken }
            });

            _logger.LogInformation("Password grant succeeded for user: {Username}", request.Username);

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        // AUTHORIZATION CODE GRANT - EXISTING
        if (request.IsAuthorizationCodeGrantType())
        {
            principal = (await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal!;

            if (principal is null)
            {
                return BadRequest(new
                {
                    error = "invalid_grant",
                    error_description = "The authorization code is invalid or expired."
                });
            }
        }
        // REFRESH TOKEN GRANT - EXISTING
        else if (request.IsRefreshTokenGrantType())
        {
            principal = (await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal!;

            if (principal is null)
            {
                return BadRequest(new
                {
                    error = "invalid_grant",
                    error_description = "The refresh token is invalid or expired."
                });
            }

            // Refresh user claims from database
            var userId = principal.GetClaim(Claims.Subject);
            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _users.FindByIdAsync(userId);
                if (user is not null)
                {
                    var roles = await _users.GetRolesAsync(user);

                    var claims = new List<Claim>
                    {
                        new(Claims.Subject, user.Id.ToString()),
                        new(Claims.Name, user.UserName ?? user.Email ?? user.Id.ToString()),
                        new(Claims.Email, user.Email ?? ""),
                        new(Claims.PhoneNumber, user.PhoneNumber ?? "")
                    };

                    foreach (var r in roles)
                        claims.Add(new Claim(Claims.Role, r));

                    if (user.DietaryPreference is not null)
                        claims.Add(new Claim("dietary_preference", user.DietaryPreference));

                    claims.Add(new Claim("reward_points", user.RewardPoints.ToString()));

                    if (roles.Contains("Admin"))
                        claims.Add(new Claim("permission", "admin"));

                    var identity = new ClaimsIdentity(claims, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                    principal = new ClaimsPrincipal(identity);

                    principal.SetScopes(request.GetScopes());
                    principal.SetResources("desicorner-api");

                    principal.SetDestinations(static claim => claim.Type switch
                    {
                        Claims.Name or Claims.Subject or Claims.Role or Claims.Email or Claims.PhoneNumber =>
                            new[] { Destinations.AccessToken, Destinations.IdentityToken },
                        "dietary_preference" or "reward_points" or "permission" =>
                            new[] { Destinations.AccessToken },
                        _ =>
                            new[] { Destinations.AccessToken }
                    });
                }
            }
        }
        else
        {
            return BadRequest(new
            {
                error = "unsupported_grant_type",
                error_description = "The specified grant type is not supported."
            });
        }

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}