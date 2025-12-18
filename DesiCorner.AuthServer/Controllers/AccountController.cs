using DesiCorner.AuthServer.Data;
using DesiCorner.AuthServer.Identity;
using DesiCorner.AuthServer.Services;
using DesiCorner.Contracts.Auth;
using DesiCorner.Contracts.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Security.Claims;

namespace DesiCorner.AuthServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private const string CombinedAuthSchemes = "Identity.Application,JwBearer";
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IOtpService _otpService;
    private readonly ITokenService _tokenService;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<AccountController> _logger;
    private readonly ApplicationDbContext _context;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IOtpService otpService,
        IConnectionMultiplexer redis,
        ILogger<AccountController> logger,ITokenService tokenService,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _otpService = otpService;
        _redis = redis;
        _logger = logger;
        _tokenService = tokenService;
        _context = context;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        // Rate limiting
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var db = _redis.GetDatabase();
        var rlKey = RedisKeys.RegisterRateLimit(ip);
        var count = await db.StringIncrementAsync(rlKey);
        if (count == 1) await db.KeyExpireAsync(rlKey, TimeSpan.FromMinutes(15));
        if (count > 5)
        {
            return StatusCode(429, new ResponseDto
            {
                IsSuccess = false,
                Message = "Too many registration attempts. Please try again later."
            });
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            DietaryPreference = request.DietaryPreference,
            EmailConfirmed = false,
            PhoneNumberConfirmed = false
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return BadRequest(new ResponseDto
            {
                IsSuccess = false,
                Message = string.Join(", ", result.Errors.Select(e => e.Description))
            });
        }

        // Add Customer role by default
        await _userManager.AddToRoleAsync(user, "Customer");

        // Send OTP for email verification
        await _otpService.SendOtpAsync(request.Email, "Registration","Email");

        _logger.LogInformation("User {Email} registered successfully", request.Email);

        return Ok(new ResponseDto
        {
            IsSuccess = true,
            Message = "Registration successful. Please verify your phone number with the OTP sent.",
            Result = new { UserId = user.Id, Email = user.Email }
        });
    }

    /// <summary>
    /// Login - Sets authentication cookie for OAuth flow AND returns JWT token
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        // Rate limiting
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var db = _redis.GetDatabase();
        var rlKey = RedisKeys.LoginRateLimit(ip);
        var count = await db.StringIncrementAsync(rlKey);
        if (count == 1) await db.KeyExpireAsync(rlKey, TimeSpan.FromMinutes(15));
        if (count > 10)
        {
            return StatusCode(429, new ResponseDto
            {
                IsSuccess = false,
                Message = "Too many login attempts. Please try again later."
            });
        }

        var user = await _userManager.FindByEmailAsync(request.Email ?? string.Empty);
        if (user is null)
        {
            return Unauthorized(new ResponseDto
            {
                IsSuccess = false,
                Message = "Invalid email or password."
            });
        }

        // Check if email is verified
        if (!user.EmailConfirmed)
        {
            return Unauthorized(new ResponseDto
            {
                IsSuccess = false,
                Message = "Please verify your email address before logging in. Check your inbox for the verification code."
            });
        }

        var result = await _signInManager.PasswordSignInAsync(
            user,
            request.Password,
            isPersistent: request.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            // Get user roles
            var roles = await _userManager.GetRolesAsync(user);

            // Generate JWT token
            var token = _tokenService.GenerateAccessToken(user, roles);

            _logger.LogInformation("User {Email} logged in successfully", request.Email);

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Message = "Login successful",
                Result = new
                {
                    token, // ← JWT Token for Angular
                    user = new
                    {
                        id = user.Id.ToString(),
                        email = user.Email,
                        phoneNumber = user.PhoneNumber,
                        dietaryPreference = user.DietaryPreference,
                        rewardPoints = user.RewardPoints,
                        roles
                    },
                    expiresIn = 1440 * 60 // 24 hours in seconds
                }
            });
        }

        if (result.IsLockedOut)
        {
            return StatusCode(423, new ResponseDto
            {
                IsSuccess = false,
                Message = "Account is locked due to multiple failed login attempts."
            });
        }

        if (result.RequiresTwoFactor)
        {
            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Message = "Two-factor authentication required.",
                Result = new { RequiresOtp = true, PhoneNumber = user.PhoneNumber }
            });
        }

        return Unauthorized(new ResponseDto
        {
            IsSuccess = false,
            Message = "Invalid email or password."
        });
    }

    /// <summary>
    /// Check if user is authenticated (for Angular to verify before OAuth redirect)
    /// </summary>
    [HttpGet("check-auth")]
    [AllowAnonymous]
    public IActionResult CheckAuth()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Message = "Authenticated",
                Result = new
                {
                    IsAuthenticated = true,
                    UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                }
            });
        }

        return Ok(new ResponseDto
        {
            IsSuccess = true,
            Message = "Not authenticated",
            Result = new { IsAuthenticated = false }
        });
    }

    /// <summary>
    /// Send OTP to phone number
    /// </summary>
    [HttpPost("send-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequestDto request)
    {
        // Determine identifier (email or phone)
        var identifier = request.DeliveryMethod.Equals("Email", StringComparison.OrdinalIgnoreCase)
            ? request.Email
            : request.PhoneNumber;

        if (string.IsNullOrWhiteSpace(identifier))
        {
            return BadRequest(new ResponseDto
            {
                IsSuccess = false,
                Message = $"{request.DeliveryMethod} is required."
            });
        }

        var sent = await _otpService.SendOtpAsync(identifier, request.Purpose, request.DeliveryMethod);

        if (!sent)
        {
            return BadRequest(new ResponseDto
            {
                IsSuccess = false,
                Message = $"Failed to send OTP via {request.DeliveryMethod}. Please try again."
            });
        }

        return Ok(new ResponseDto
        {
            IsSuccess = true,
            Message = $"OTP sent successfully via {request.DeliveryMethod}. Valid for 10 minutes."
        });
    }

    /// <summary>
    /// Verify OTP
    /// </summary>
    [HttpPost("verify-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequestDto request)
    {
        var (isValid, error) = await _otpService.ValidateOtpAsync(request.Identifier, request.Otp);

        if (!isValid)
        {
            return BadRequest(new ResponseDto
            {
                IsSuccess = false,
                Message = error ?? "Invalid OTP"
            });
        }

        // Mark phone or email as confirmed
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == request.Identifier || u.Email == request.Identifier);

        if (user is not null)
        {
            // Check if it's a phone number or email
            if (request.Identifier.Contains('@'))
            {
                user.EmailConfirmed = true;
            }
            else
            {
                user.PhoneNumberConfirmed = true;
            }
            await _userManager.UpdateAsync(user);
        }

        return Ok(new ResponseDto
        {
            IsSuccess = true,
            Message = "Verification successful."
        });
    }

    /// <summary>
    /// Check if user exists by email or phone (for guest checkout user linking)
    /// </summary>
    [HttpGet("user-lookup")]
    [AllowAnonymous]
    public async Task<IActionResult> UserLookup([FromQuery] string? email, [FromQuery] string? phone)
    {
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phone))
        {
            return BadRequest(new ResponseDto
            {
                IsSuccess = false,
                Message = "Either email or phone is required"
            });
        }

        ApplicationUser? user = null;

        if (!string.IsNullOrWhiteSpace(email))
        {
            user = await _userManager.FindByEmailAsync(email);
        }
        else if (!string.IsNullOrWhiteSpace(phone))
        {
            user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == phone);
        }

        return Ok(new ResponseDto
        {
            IsSuccess = true,
            Result = new
            {
                UserId = user?.Id,
                Email = user?.Email,
                PhoneNumber = user?.PhoneNumber,
                Exists = user != null
            }
        });
    }

    /// <summary>
    /// Logout - Clears authentication cookie
    /// </summary>
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok(new ResponseDto
        {
            IsSuccess = true,
            Message = "Logged out successfully"
        });
    }

    // <summary>
    /// Get current user profile - Supports both Cookie and JWT authentication
    /// </summary>
    [Authorize]
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new ResponseDto
            {
                IsSuccess = false,
                Message = "User not authenticated"
            });
        }

        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return NotFound(new ResponseDto
            {
                IsSuccess = false,
                Message = "User not found"
            });
        }

        var roles = await _userManager.GetRolesAsync(user);

        // Load addresses
        var addresses = await _userManager.Users
            .Where(u => u.Id == user.Id)
            .SelectMany(u => u.DeliveryAddresses)
            .Select(a => new DeliveryAddressDto
            {
                Id = a.Id,
                Label = a.Label,
                AddressLine1 = a.AddressLine1,
                AddressLine2 = a.AddressLine2,
                City = a.City,
                State = a.State,
                ZipCode = a.ZipCode,
                IsDefault = a.IsDefault
            })
            .ToListAsync();

        var userDto = new UserDto
        {
            Id = user.Id,
            Email = user.Email!,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            DietaryPreference = user.DietaryPreference,
            RewardPoints = user.RewardPoints,
            Addresses = addresses,
            Roles = roles.ToList()
        };

        return Ok(new ResponseDto
        {
            IsSuccess = true,
            Result = userDto
        });
    }

    // <summary>
    /// Add delivery address
    /// </summary>
    [Authorize]
    [HttpPost("addresses")]
    public async Task<IActionResult> AddAddress([FromBody] AddAddressDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            return Unauthorized();
        }

        // If this is set as default, unset all other defaults
        if (request.IsDefault)
        {
            var existingAddresses = await _context.DeliveryAddresses
                .Where(a => a.UserId == userGuid)
                .ToListAsync();

            foreach (var addr in existingAddresses)
            {
                addr.IsDefault = false;
            }
        }

        var address = new DeliveryAddress
        {
            Id = Guid.NewGuid(),
            UserId = userGuid,
            Label = request.Label,
            AddressLine1 = request.AddressLine1,
            AddressLine2 = request.AddressLine2,
            City = request.City,
            State = request.State,
            ZipCode = request.ZipCode,
            IsDefault = request.IsDefault
        };

        _context.DeliveryAddresses.Add(address);
        await _context.SaveChangesAsync();

        return Ok(new ResponseDto
        {
            IsSuccess = true,
            Message = "Address added successfully",
            Result = new DeliveryAddressDto
            {
                Id = address.Id,
                Label = address.Label,
                AddressLine1 = address.AddressLine1,
                AddressLine2 = address.AddressLine2,
                City = address.City,
                State = address.State,
                ZipCode = address.ZipCode,
                IsDefault = address.IsDefault
            }
        });
    }

    // <summary>
    /// Change password
    /// </summary>
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

        if (!result.Succeeded)
        {
            return BadRequest(new ResponseDto
            {
                IsSuccess = false,
                Message = string.Join(", ", result.Errors.Select(e => e.Description))
            });
        }

        return Ok(new ResponseDto
        {
            IsSuccess = true,
            Message = "Password changed successfully"
        });
    }
}