using ReactCore.Backend.Models;

namespace ReactCore.Backend.Repositories;

public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken cancellationToken = default);
    Task<Order?> GetByOrderNumberAsync(Guid userId, string orderNumber, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Order> Items, int Total)> GetOrdersAsync(Guid userId, string? status, int skip, int take, CancellationToken cancellationToken = default);
}
