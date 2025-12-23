namespace DesCorner.Contracts.Auth;

public class AdminUserListDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public string? DietaryPreference { get; set; }
    public int RewardPoints { get; set; }
    public List<string> Roles { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsLocked { get; set; }
}

public class AdminUserFilterDto
{
    public string? SearchTerm { get; set; } // Email or phone
    public string? Role { get; set; }
    public bool? IsLocked { get; set; }
    public bool? EmailConfirmed { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
}

public class UpdateUserRoleDto
{
    public Guid UserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool Add { get; set; } = true; // true = add role, false = remove role
}

public class ToggleUserLockDto
{
    public Guid UserId { get; set; }
    public bool Lock { get; set; }
    public string? Reason { get; set; }
}

public class UserStatsDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; } // Logged in within 30 days
    public int NewUsersToday { get; set; }
    public int NewUsersThisWeek { get; set; }
    public int NewUsersThisMonth { get; set; }
    public int AdminCount { get; set; }
    public int CustomerCount { get; set; }
    public int LockedUsers { get; set; }
    public int UnverifiedUsers { get; set; }
}