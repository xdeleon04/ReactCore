namespace ReactCore.Backend.Models.Dto;

public class SalesReportDto
{
    public SalesReportDateRangeDto DateRange { get; set; } = new();
    public SalesReportSummaryDto Summary { get; set; } = new();
    public List<TopProductDto> TopProducts { get; set; } = new();
    public List<OrderStatusBreakdownDto> OrderStatusBreakdown { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

public class SalesReportDateRangeDto
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
}

public class SalesReportSummaryDto
{
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public decimal AverageOrderValue { get; set; }
    public int TotalItemsSold { get; set; }
    public int UniqueCustomers { get; set; }
}

public class TopProductDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int UnitsSold { get; set; }
    public decimal Revenue { get; set; }
}

public class OrderStatusBreakdownDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}
