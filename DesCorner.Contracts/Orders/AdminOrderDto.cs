namespace DesCorner.Contracts.Orders;

public class AdminOrderListDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public bool IsGuestOrder { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public int ItemCount { get; set; }
}

public class AdminOrderFilterDto
{
    public string? Status { get; set; }
    public string? PaymentStatus { get; set; }
    public string? SearchTerm { get; set; } // Order number or email
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "OrderDate";
    public bool SortDescending { get; set; } = true;
}

public class OrderStatsDto
{
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public int ProcessingOrders { get; set; }
    public int DeliveredOrders { get; set; }
    public int CancelledOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TodayRevenue { get; set; }
    public decimal WeekRevenue { get; set; }
    public decimal MonthRevenue { get; set; }
}
