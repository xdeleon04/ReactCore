using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ReactCore.Backend.Data;
using ReactCore.Backend.Models;
using ReactCore.Backend.Models.Dto;
using ReactCore.Backend.Models.Enums;
using ReactCore.Backend.Repositories;
using ReactCore.Backend.Validators;

namespace ReactCore.Backend.Services.Admin;

public interface IAdminOrderService
{
    Task<(List<AdminOrderListItemDto> Items, int Total)> ListOrdersAsync(
        string? orderNumber,
        string? email,
        string? status,
        DateTime? startDate,
        DateTime? endDate,
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<AdminOrderDetailDto?> GetOrderDetailAsync(int orderId, CancellationToken cancellationToken);

    Task<(bool Success, object? Body, object? ErrorBody)> UpdateStatusAsync(
        Guid adminUserId,
        int orderId,
        string newStatus,
        string? reason,
        string? ipAddress,
        CancellationToken cancellationToken);
}

public class AdminOrderService : IAdminOrderService
{
    private readonly AppDbContext _db;
    private readonly IOrderRepository _orders;

    public AdminOrderService(AppDbContext db, IOrderRepository orders)
    {
        _db = db;
        _orders = orders;
    }

    public async Task<(List<AdminOrderListItemDto> Items, int Total)> ListOrdersAsync(
        string? orderNumber,
        string? email,
        string? status,
        DateTime? startDate,
        DateTime? endDate,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        take = Math.Clamp(take, 1, 100);
        skip = Math.Max(0, skip);

        var query = _orders.QueryForAdmin(orderNumber, email, status, startDate, endDate);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Select(o => new AdminOrderListItemDto(
                o.Id,
                o.OrderNumber,
                o.Email,
                o.TotalPrice,
                o.Status,
                o.CreatedAt))
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<AdminOrderDetailDto?> GetOrderDetailAsync(int orderId, CancellationToken cancellationToken)
    {
        if (orderId < 1)
        {
            return null;
        }

        var order = await _orders.GetByIdForAdminAsync(orderId, cancellationToken);
        if (order is null)
        {
            return null;
        }

        var items = (order.Items ?? new List<OrderItem>())
            .Select(i => new AdminOrderItemDetailDto(
                i.ProductId,
                i.ProductName,
                i.Quantity,
                i.UnitPrice,
                i.LineTotal))
            .ToList();

        var allowed = OrderStatusTransitions.GetAllowedNext(order.Status).ToList();

        return new AdminOrderDetailDto(
            order.Id,
            order.OrderNumber,
            order.Email,
            order.Status,
            order.CreatedAt,
            items,
            order.SubtotalPrice,
            order.TotalPrice,
            allowed);
    }

    public async Task<(bool Success, object? Body, object? ErrorBody)> UpdateStatusAsync(
        Guid adminUserId,
        int orderId,
        string newStatus,
        string? reason,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        if (orderId < 1)
        {
            return (false, null, new { error = "Validation failed", statusCode = 400 });
        }

        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
        if (order is null)
        {
            return (false, null, new { error = "Order not found", statusCode = 404 });
        }

        var normalizedNewStatus = OrderStatusTransitions.Normalize(newStatus);

        if (!OrderStatusTransitions.CanTransition(order.Status, normalizedNewStatus))
        {
            return (false, null, new
            {
                error = "Invalid status transition",
                message = $"Cannot transition from {order.Status} to {normalizedNewStatus} (backwards transition not allowed)",
                statusCode = 400
            });
        }

        var oldValues = JsonSerializer.Serialize(new { status = order.Status });

        order.Status = normalizedNewStatus;
        order.UpdatedAt = now;

        var newValues = JsonSerializer.Serialize(new { status = order.Status });

        _db.AdminActions.Add(new AdminAction
        {
            AdminUserId = adminUserId,
            ActionType = AdminActionType.OrderStatusChange.ToString(),
            EntityType = EntityType.Order.ToString(),
            EntityId = order.Id.ToString(),
            OldValues = oldValues,
            NewValues = newValues,
            Timestamp = now,
            IpAddress = ipAddress,
            Reason = reason,
        });

        await _db.SaveChangesAsync(cancellationToken);

        return (true, new { success = true, orderId = order.Id, status = order.Status, updatedAt = order.UpdatedAt }, null);
    }
}
