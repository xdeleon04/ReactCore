using Microsoft.EntityFrameworkCore;
using ReactCore.Backend.Data;
using ReactCore.Backend.Models;
using ReactCore.Backend.Models.Dto;
using ReactCore.Backend.Repositories;
using System.Data;

namespace ReactCore.Backend.Services;

public class OrderService : IOrderService
{
    private readonly AppDbContext _context;
    private readonly ICartRepository _cartRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        AppDbContext context,
        ICartRepository cartRepository,
        IOrderRepository orderRepository,
        IInventoryService inventoryService,
        ILogger<OrderService> logger)
    {
        _context = context;
        _cartRepository = cartRepository;
        _orderRepository = orderRepository;
        _inventoryService = inventoryService;
        _logger = logger;
    }

    public async Task<OrderDto> CreateOrderAsync(Guid userId, CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var cart = await _cartRepository.GetOrCreateAsync(userId, cancellationToken);

        if (cart.Id != request.CartId)
        {
            throw new InvalidCartException("Cart does not belong to user");
        }

        if (cart.Items.Count == 0)
        {
            throw new EmptyCartException("Cart must contain at least one item");
        }

        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;

        var lockedProducts = new Dictionary<int, Product>();
        var conflicts = new List<InventoryConflictDto>();

        foreach (var cartItem in cart.Items)
        {
            var product = await _inventoryService.GetProductForUpdateAsync(cartItem.ProductId, cancellationToken);
            if (product is null || product.IsDeleted)
            {
                conflicts.Add(new InventoryConflictDto(
                    CartItemId: cartItem.Id,
                    ProductId: cartItem.ProductId,
                    ProductName: cartItem.Product?.Name ?? "Unknown",
                    RequestedQuantity: cartItem.Quantity,
                    AvailableQuantity: 0,
                    Action: "REJECT"));
                continue;
            }

            lockedProducts[product.Id] = product;

            if (product.StockQuantity < cartItem.Quantity)
            {
                conflicts.Add(new InventoryConflictDto(
                    CartItemId: cartItem.Id,
                    ProductId: product.Id,
                    ProductName: product.Name,
                    RequestedQuantity: cartItem.Quantity,
                    AvailableQuantity: product.StockQuantity,
                    Action: "REJECT"));
            }
        }

        if (conflicts.Count > 0)
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            throw new InventoryConflictException(
                "Order contains items with insufficient inventory",
                conflicts);
        }

        var orderNumber = await GenerateOrderNumberAsync(cancellationToken);

        var order = new Order
        {
            UserId = userId,
            Email = request.Email,
            OrderNumber = orderNumber,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        foreach (var cartItem in cart.Items)
        {
            var product = lockedProducts[cartItem.ProductId];
            var unitPrice = cartItem.UnitPrice;
            var lineTotal = unitPrice * cartItem.Quantity;

            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = cartItem.Quantity,
                UnitPrice = unitPrice,
                LineTotal = lineTotal
            });
        }

        order.SubtotalPrice = order.Items.Sum(i => i.LineTotal);
        order.TotalPrice = order.SubtotalPrice;

        _context.Orders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);

        foreach (var cartItem in cart.Items)
        {
            var product = lockedProducts[cartItem.ProductId];

            var previous = product.StockQuantity;
            product.StockQuantity -= cartItem.Quantity;

            _context.InventoryAudits.Add(new InventoryAudit
            {
                ProductId = product.Id,
                PreviousQuantity = previous,
                NewQuantity = product.StockQuantity,
                Reason = "ORDER_PLACED",
                OrderId = order.Id,
                Timestamp = DateTime.UtcNow
            });
        }

        _context.CartItems.RemoveRange(cart.Items);
        cart.Items.Clear();
        cart.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        _logger.LogInformation("Created order {OrderNumber} for user {UserId}", order.OrderNumber, userId);

        return MapOrder(order);
    }

    public async Task<OrderDto?> GetOrderAsync(Guid userId, string orderNumber, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByOrderNumberAsync(userId, orderNumber, cancellationToken);
        return order is null ? null : MapOrder(order);
    }

    public async Task<OrderListDto> GetUserOrdersAsync(Guid userId, string? status, int skip, int take, CancellationToken cancellationToken = default)
    {
        var (items, total) = await _orderRepository.GetOrdersAsync(userId, status, skip, take, cancellationToken);

        var list = items.Select(o => new OrderListItemDto(
            Id: o.Id,
            OrderNumber: o.OrderNumber,
            Subtotal: o.SubtotalPrice,
            Total: o.TotalPrice,
            Status: o.Status,
            ItemCount: o.Items.Count,
            CreatedAt: o.CreatedAt
        )).ToList();

        return new OrderListDto(list, total, skip, take);
    }

    private async Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken)
    {
        var prefix = $"ORD-{DateTime.UtcNow:yyyyMMdd}-";
        var countToday = await _context.Orders
            .CountAsync(o => o.OrderNumber.StartsWith(prefix), cancellationToken);

        return $"{prefix}{countToday + 1:000}";
    }

    private static OrderDto MapOrder(Order order)
    {
        var items = order.Items
            .Select(i => new OrderItemDto(
                Id: i.Id,
                ProductId: i.ProductId,
                ProductName: i.ProductName,
                Quantity: i.Quantity,
                UnitPrice: i.UnitPrice,
                LineTotal: i.LineTotal
            ))
            .ToList();

        return new OrderDto(
            Id: order.Id,
            OrderNumber: order.OrderNumber,
            UserId: order.UserId,
            Email: order.Email,
            Subtotal: order.SubtotalPrice,
            Total: order.TotalPrice,
            Status: order.Status,
            Items: items,
            CreatedAt: order.CreatedAt,
            UpdatedAt: order.UpdatedAt
        );
    }
}
