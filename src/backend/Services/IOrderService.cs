using ReactCore.Backend.Models.Dto;

namespace ReactCore.Backend.Services;

public interface IOrderService
{
    Task<OrderDto> CreateOrderAsync(Guid userId, CreateOrderRequest request, CancellationToken cancellationToken = default);
    Task<OrderDto?> GetOrderAsync(Guid userId, string orderNumber, CancellationToken cancellationToken = default);
    Task<OrderListDto> GetUserOrdersAsync(Guid userId, string? status, int skip, int take, CancellationToken cancellationToken = default);
}
