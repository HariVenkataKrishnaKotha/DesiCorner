using DesCorner.Contracts.Auth;
using DesiCorner.AuthServer.Identity;
using DesiCorner.Contracts.Auth;
using DesiCorner.Contracts.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;

namespace DesiCorner.AuthServer.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ILogger<AdminController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    /// <summary>
    /// Get all users with filtering and pagination
    /// </summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers([FromQuery] AdminUserFilterDto filter)
    {
        try
        {
            var query = _userManager.Users.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                var term = filter.SearchTerm.ToLower();
                query = query.Where(u =>
                    u.Email!.ToLower().Contains(term) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(term)));
            }

            if (filter.EmailConfirmed.HasValue)
            {
                query = query.Where(u => u.EmailConfirmed == filter.EmailConfirmed.Value);
            }

            if (filter.IsLocked.HasValue)
            {
                if (filter.IsLocked.Value)
                {
                    query = query.Where(u => u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow);
                }
                else
                {
                    query = query.Where(u => u.LockoutEnd == null || u.LockoutEnd <= DateTimeOffset.UtcNow);
                }
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply sorting
            query = filter.SortBy.ToLower() switch
            {
                "email" => filter.SortDescending
                    ? query.OrderByDescending(u => u.Email)
                    : query.OrderBy(u => u.Email),
                "lastloginat" => filter.SortDescending
                    ? query.OrderByDescending(u => u.LastLoginAt)
                    : query.OrderBy(u => u.LastLoginAt),
                "rewardpoints" => filter.SortDescending
                    ? query.OrderByDescending(u => u.RewardPoints)
                    : query.OrderBy(u => u.RewardPoints),
                _ => filter.SortDescending
                    ? query.OrderByDescending(u => u.CreatedAt)
                    : query.OrderBy(u => u.CreatedAt)
            };

            // Apply pagination
            var users = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            // Map to DTOs with roles
            var userDtos = new List<AdminUserListDto>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                // Filter by role if specified
                if (!string.IsNullOrEmpty(filter.Role) && !roles.Contains(filter.Role))
                {
                    continue;
                }

                userDtos.Add(new AdminUserListDto
                {
                    Id = user.Id,
                    Email = user.Email!,
                    PhoneNumber = user.PhoneNumber,
                    EmailConfirmed = user.EmailConfirmed,
                    PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                    DietaryPreference = user.DietaryPreference,
                    RewardPoints = user.RewardPoints,
                    Roles = roles.ToList(),
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt,
                    IsLocked = user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.UtcNow
                });
            }

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Result = new
                {
                    Users = userDtos,
                    TotalCount = totalCount,
                    Page = filter.Page,
                    PageSize = filter.PageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users");
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "Failed to retrieve users"
            });
        }
    }

    /// <summary>
    /// Get user details by ID
    /// </summary>
    [HttpGet("users/{userId}")]
    public async Task<IActionResult> GetUserById(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user == null)
        {
            return NotFound(new ResponseDto
            {
                IsSuccess = false,
                Message = "User not found"
            });
        }

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new ResponseDto
        {
            IsSuccess = true,
            Result = new AdminUserListDto
            {
                Id = user.Id,
                Email = user.Email!,
                PhoneNumber = user.PhoneNumber,
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                DietaryPreference = user.DietaryPreference,
                RewardPoints = user.RewardPoints,
                Roles = roles.ToList(),
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                IsLocked = user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.UtcNow
            }
        });
    }

    /// <summary>
    /// Update user role (add or remove)
    /// </summary>
    [HttpPost("users/role")]
    public async Task<IActionResult> UpdateUserRole([FromBody] UpdateUserRoleDto request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());

        if (user == null)
        {
            return NotFound(new ResponseDto
            {
                IsSuccess = false,
                Message = "User not found"
            });
        }

        // Check if role exists
        if (!await _roleManager.RoleExistsAsync(request.Role))
        {
            return BadRequest(new ResponseDto
            {
                IsSuccess = false,
                Message = $"Role '{request.Role}' does not exist"
            });
        }

        IdentityResult result;
        if (request.Add)
        {
            result = await _userManager.AddToRoleAsync(user, request.Role);
        }
        else
        {
            result = await _userManager.RemoveFromRoleAsync(user, request.Role);
        }

        if (!result.Succeeded)
        {
            return BadRequest(new ResponseDto
            {
                IsSuccess = false,
                Message = string.Join(", ", result.Errors.Select(e => e.Description))
            });
        }

        _logger.LogInformation("Admin {Admin} {Action} role {Role} for user {User}",
            User.Identity?.Name, request.Add ? "added" : "removed", request.Role, user.Email);

        return Ok(new ResponseDto
        {
            IsSuccess = true,
            Message = $"Role '{request.Role}' {(request.Add ? "added to" : "removed from")} user successfully"
        });
    }

    /// <summary>
    /// Lock or unlock a user account
    /// </summary>
    [HttpPost("users/lock")]
    public async Task<IActionResult> ToggleUserLock([FromBody] ToggleUserLockDto request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());

        if (user == null)
        {
            return NotFound(new ResponseDto
            {
                IsSuccess = false,
                Message = "User not found"
            });
        }

        if (request.Lock)
        {
            // Lock user for 100 years (effectively permanent until unlocked)
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
            _logger.LogWarning("Admin {Admin} locked user {User}. Reason: {Reason}",
                User.Identity?.Name, user.Email, request.Reason ?? "Not specified");
        }
        else
        {
            // Unlock user
            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);
            _logger.LogInformation("Admin {Admin} unlocked user {User}",
                User.Identity?.Name, user.Email);
        }

        return Ok(new ResponseDto
        {
            IsSuccess = true,
            Message = $"User account {(request.Lock ? "locked" : "unlocked")} successfully"
        });
    }

    /// <summary>
    /// Get user statistics
    /// </summary>
    [HttpGet("users/stats")]
    public async Task<IActionResult> GetUserStats()
    {
        try
        {
            var today = DateTime.UtcNow.Date;
            var weekAgo = today.AddDays(-7);
            var monthAgo = today.AddDays(-30);
            var activeThreshold = today.AddDays(-30);

            var allUsers = await _userManager.Users.ToListAsync();

            var stats = new UserStatsDto
            {
                TotalUsers = allUsers.Count,
                ActiveUsers = allUsers.Count(u => u.LastLoginAt >= activeThreshold),
                NewUsersToday = allUsers.Count(u => u.CreatedAt.Date == today),
                NewUsersThisWeek = allUsers.Count(u => u.CreatedAt >= weekAgo),
                NewUsersThisMonth = allUsers.Count(u => u.CreatedAt >= monthAgo),
                LockedUsers = allUsers.Count(u => u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow),
                UnverifiedUsers = allUsers.Count(u => !u.EmailConfirmed)
            };

            // Count by roles
            foreach (var user in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Admin"))
                    stats.AdminCount++;
                if (roles.Contains("Customer"))
                    stats.CustomerCount++;
            }

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Result = stats
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user stats");
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "Failed to retrieve user statistics"
            });
        }
    }

    /// <summary>
    /// Get all available roles
    /// </summary>
    [HttpGet("roles")]
    public async Task<IActionResult> GetAllRoles()
    {
        var roles = await _roleManager.Roles
            .Select(r => new { r.Id, r.Name })
            .ToListAsync();

        return Ok(new ResponseDto
        {
            IsSuccess = true,
            Result = roles
        });
    }

    /// <summary>
    /// Get recent user registrations
    /// </summary>
    [HttpGet("users/recent")]
    public async Task<IActionResult> GetRecentUsers([FromQuery] int count = 5)
    {
        var users = await _userManager.Users
            .OrderByDescending(u => u.CreatedAt)
            .Take(count)
            .Select(u => new
            {
                UserId = u.Id,
                Email = u.Email,
                RegisteredAt = u.CreatedAt
            })
            .ToListAsync();

        return Ok(new ResponseDto
        {
            IsSuccess = true,
            Result = users
        });
    }
}