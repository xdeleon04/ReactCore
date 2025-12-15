using Microsoft.EntityFrameworkCore;
using ReactCore.Backend.Data;
using ReactCore.Backend.Models;

namespace ReactCore.Backend.Repositories;

public interface IAdminActionRepository
{
    Task AddAsync(AdminAction action);
    IQueryable<AdminAction> Query();
}

public class AdminActionRepository : IAdminActionRepository
{
    private readonly AppDbContext _context;

    public AdminActionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AdminAction action)
    {
        _context.Set<AdminAction>().Add(action);
        await _context.SaveChangesAsync();
    }

    public IQueryable<AdminAction> Query()
    {
        return _context.Set<AdminAction>().AsNoTracking();
    }
}
