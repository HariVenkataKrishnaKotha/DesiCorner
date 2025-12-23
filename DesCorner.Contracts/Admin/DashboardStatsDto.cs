namespace DesiCorner.Contracts.Admin;

public class DashboardStatsDto
{
    // Order Stats
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public int ProcessingOrders { get; set; }
    public int CompletedOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TodayRevenue { get; set; }

    // User Stats
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int NewUsersToday { get; set; }

    // Product Stats
    public int TotalProducts { get; set; }
    public int OutOfStockProducts { get; set; }
    public int LowStockProducts { get; set; }

    // Coupon Stats
    public int ActiveCoupons { get; set; }
    public int TotalCouponsUsed { get; set; }

    // Recent Activity
    public List<RecentOrderDto> RecentOrders { get; set; } = new();
    public List<RecentUserDto> RecentUsers { get; set; } = new();
}

public class RecentOrderDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class RecentUserDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
}