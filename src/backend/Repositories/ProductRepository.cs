using Microsoft.EntityFrameworkCore;
using ReactCore.Backend.Data;
using ReactCore.Backend.Models;

namespace ReactCore.Backend.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context)
    {
        _context = context;
    }

    public IQueryable<Product> QueryActive()
    {
        return _context.Products
            .AsNoTracking()
            .Where(p => !p.IsDeleted);
    }

    public Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _context.Products
            .AsNoTracking()
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
    }
}
