using ReactCore.Backend.Models;

namespace ReactCore.Backend.Repositories;

public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken cancellationToken = default);
    Task<Order?> GetByOrderNumberAsync(Guid userId, string orderNumber, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Order> Items, int Total)> GetOrdersAsync(Guid userId, string? status, int skip, int take, CancellationToken cancellationToken = default);

    IQueryable<Order> QueryForAdmin(string? orderNumber, string? email, string? status, DateTime? startDate, DateTime? endDate);
    Task<Order?> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default);
    Task UpdateAsync(Order order, CancellationToken cancellationToken = default);
}
