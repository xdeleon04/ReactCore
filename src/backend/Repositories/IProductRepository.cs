using ReactCore.Backend.Models;

namespace ReactCore.Backend.Repositories;

public interface IProductRepository
{
    IQueryable<Product> QueryActive();
    Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
