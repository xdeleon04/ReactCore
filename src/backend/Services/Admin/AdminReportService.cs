using System.Globalization;
using System.Text;
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using ReactCore.Backend.Data;
using ReactCore.Backend.Models.Dto;

namespace ReactCore.Backend.Services.Admin;

public interface IAdminReportService
{
    Task<SalesReportDto> GetSalesReportAsync(DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken);
    Task<(byte[] CsvBytes, string FileName)> ExportSalesReportCsvAsync(DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken);
}

public class AdminReportService : IAdminReportService
{
    private readonly AppDbContext _db;

    public AdminReportService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<SalesReportDto> GetSalesReportAsync(DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken)
    {
        var generatedAt = DateTime.UtcNow;

        var ordersQuery = _db.Orders
            .AsNoTracking()
            .Where(o => o.CreatedAt >= startUtc && o.CreatedAt <= endUtc);

        var totalOrders = await ordersQuery.CountAsync(cancellationToken);

        var totalRevenue = totalOrders == 0
            ? 0m
            : await ordersQuery.SumAsync(o => o.TotalPrice, cancellationToken);

        var averageOrderValue = totalOrders == 0 ? 0m : totalRevenue / totalOrders;

        var totalItemsSold = await _db.OrderItems
            .AsNoTracking()
            .Where(oi => oi.Order != null && oi.Order.CreatedAt >= startUtc && oi.Order.CreatedAt <= endUtc)
            .SumAsync(oi => (int?)oi.Quantity, cancellationToken) ?? 0;

        var uniqueCustomers = await ordersQuery
            .Select(o => o.Email)
            .Distinct()
            .CountAsync(cancellationToken);

        var topProducts = await _db.OrderItems
            .AsNoTracking()
            .Where(oi => oi.Order != null && oi.Order.CreatedAt >= startUtc && oi.Order.CreatedAt <= endUtc)
            .GroupBy(oi => new { oi.ProductId, oi.ProductName })
            .Select(g => new TopProductDto
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductName,
                UnitsSold = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.LineTotal)
            })
            .OrderByDescending(p => p.Revenue)
            .ThenByDescending(p => p.UnitsSold)
            .Take(10)
            .ToListAsync(cancellationToken);

        var statusCounts = await ordersQuery
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var statusBreakdown = statusCounts
            .OrderByDescending(x => x.Count)
            .Select(x => new OrderStatusBreakdownDto
            {
                Status = x.Status,
                Count = x.Count,
                Percentage = totalOrders == 0 ? 0m : Math.Round(((decimal)x.Count / totalOrders) * 100m, 1)
            })
            .ToList();

        return new SalesReportDto
        {
            DateRange = new SalesReportDateRangeDto { Start = startUtc, End = endUtc },
            Summary = new SalesReportSummaryDto
            {
                TotalRevenue = totalRevenue,
                TotalOrders = totalOrders,
                AverageOrderValue = averageOrderValue,
                TotalItemsSold = totalItemsSold,
                UniqueCustomers = uniqueCustomers,
            },
            TopProducts = topProducts,
            OrderStatusBreakdown = statusBreakdown,
            GeneratedAt = generatedAt,
        };
    }

    public async Task<(byte[] CsvBytes, string FileName)> ExportSalesReportCsvAsync(DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken)
    {
        var orders = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.CreatedAt >= startUtc && o.CreatedAt <= endUtc)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

        var fileName = $"sales-report-{startUtc:yyyy-MM-dd}-to-{endUtc:yyyy-MM-dd}.csv";

        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        csv.WriteField("OrderNumber");
        csv.WriteField("Date");
        csv.WriteField("CustomerEmail");
        csv.WriteField("Subtotal");
        csv.WriteField("Total");
        csv.WriteField("Status");
        csv.WriteField("Items");
        csv.NextRecord();

        foreach (var order in orders)
        {
            var items = string.Join(", ", order.Items
                .OrderBy(i => i.Id)
                .Select(i => $"{i.ProductName} (qty: {i.Quantity})"));

            csv.WriteField(order.OrderNumber);
            csv.WriteField(FormatUtc(order.CreatedAt));
            csv.WriteField(order.Email);
            csv.WriteField(order.SubtotalPrice);
            csv.WriteField(order.TotalPrice);
            csv.WriteField(order.Status);
            csv.WriteField(items);
            csv.NextRecord();
        }

        await writer.FlushAsync();
        var bytes = memoryStream.ToArray();

        return (bytes, fileName);
    }

    private static string FormatUtc(DateTime dt)
    {
        var utc = dt.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(dt, DateTimeKind.Utc)
            : dt.ToUniversalTime();

        return utc.ToString("o", CultureInfo.InvariantCulture);
    }
}
