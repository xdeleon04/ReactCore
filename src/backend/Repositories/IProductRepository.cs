using ReactCore.Backend.Models;

namespace ReactCore.Backend.Repositories;

public interface IProductRepository
{
    IQueryable<Product> QueryActive();
    Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    IQueryable<Product> QueryForAdmin(string? search, string? category, bool includeArchived, bool? lowStockOnly);
    Task<Product?> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default);
    Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default);
    Task UpdateAsync(Product product, CancellationToken cancellationToken = default);
}
