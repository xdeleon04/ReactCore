namespace ReactCore.Backend.Models.Dto;

public class DashboardSummaryDto
{
    public UserMetricsDto UserMetrics { get; set; } = new();
    public ProductMetricsDto ProductMetrics { get; set; } = new();
    public OrderMetricsDto OrderMetrics { get; set; } = new();
}

public class UserMetricsDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int InactiveUsers { get; set; }
    public int NewUsersThisMonth { get; set; }
}

public class ProductMetricsDto
{
    public int TotalProducts { get; set; }
    public int ActiveProducts { get; set; }
    public int ArchivedProducts { get; set; }
    public int LowStockCount { get; set; }
}

public class OrderMetricsDto
{
    public int PendingOrders { get; set; }
    public int ProcessingOrders { get; set; }
    public int TotalOrdersToday { get; set; }
    public decimal TodayRevenue { get; set; }
}
