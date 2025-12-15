using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ReactCore.Backend.Data;
using ReactCore.Backend.Models;
using ReactCore.Backend.Models.Dto;
using ReactCore.Backend.Models.Enums;
using ReactCore.Backend.Repositories;

namespace ReactCore.Backend.Services.Admin;

public interface IAdminProductService
{
    Task<(List<AdminProductListItemDto> Items, int Total)> ListProductsAsync(string? search, string? category, bool includeArchived, bool? lowStockOnly, int skip, int take, CancellationToken cancellationToken);
    Task<AdminProductDetailDto?> GetProductDetailAsync(int productId, CancellationToken cancellationToken);
    Task<(bool Success, int? ProductId, object? Body, object? ErrorBody)> CreateAsync(Guid adminUserId, AdminProductUpsertRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task<(bool Success, object? Body, object? ErrorBody)> UpdateAsync(Guid adminUserId, int productId, AdminProductUpsertRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task<(bool Success, object? ErrorBody)> SoftDeleteAsync(Guid adminUserId, int productId, string? ipAddress, CancellationToken cancellationToken);
}

public class AdminProductService : IAdminProductService
{
    private readonly AppDbContext _db;
    private readonly IProductRepository _products;

    public AdminProductService(AppDbContext db, IProductRepository products)
    {
        _db = db;
        _products = products;
    }

    public async Task<(List<AdminProductListItemDto> Items, int Total)> ListProductsAsync(string? search, string? category, bool includeArchived, bool? lowStockOnly, int skip, int take, CancellationToken cancellationToken)
    {
        take = Math.Clamp(take, 1, 100);
        skip = Math.Max(0, skip);

        var query = _products.QueryForAdmin(search, category, includeArchived, lowStockOnly);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.Name)
            .Skip(skip)
            .Take(take)
            .Select(p => new AdminProductListItemDto(
                p.Id,
                p.Name,
                p.Price,
                p.Category,
                p.StockQuantity,
                p.ReorderLevel,
                p.IsDeleted,
                p.UpdatedAt))
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<AdminProductDetailDto?> GetProductDetailAsync(int productId, CancellationToken cancellationToken)
    {
        var product = await _products.GetByIdForAdminAsync(productId, cancellationToken);
        if (product is null)
        {
            return null;
        }

        var imageUrls = new List<string>();
        if (!string.IsNullOrWhiteSpace(product.ImageUrl))
        {
            imageUrls.Add(product.ImageUrl);
        }

        if (product.Images is not null)
        {
            imageUrls.AddRange(product.Images
                .Select(i => i.ImageUrl)
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Distinct());
        }

        IReadOnlyDictionary<string, object?> specifications = new Dictionary<string, object?>();
        if (!string.IsNullOrWhiteSpace(product.Specifications))
        {
            try
            {
                specifications = JsonSerializer.Deserialize<Dictionary<string, object?>>(product.Specifications) ?? new Dictionary<string, object?>();
            }
            catch
            {
                specifications = new Dictionary<string, object?>();
            }
        }

        return new AdminProductDetailDto(
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.Category,
            product.ImageUrl,
            imageUrls,
            specifications,
            product.StockQuantity,
            product.ReorderLevel,
            product.IsDeleted,
            product.CreatedAt,
            product.UpdatedAt);
    }

    public async Task<(bool Success, int? ProductId, object? Body, object? ErrorBody)> CreateAsync(Guid adminUserId, AdminProductUpsertRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var product = new Product
        {
            Name = request.Name.Trim(),
            Category = request.Category.Trim(),
            Description = request.Description,
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            ReorderLevel = request.ReorderLevel ?? 5,
            IsDeleted = false,
            ImageUrl = request.ImageUrl,
            Specifications = request.Specifications is null ? null : JsonSerializer.Serialize(request.Specifications),
            CreatedAt = now,
            UpdatedAt = now,
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync(cancellationToken);

        _db.AdminActions.Add(new AdminAction
        {
            AdminUserId = adminUserId,
            ActionType = AdminActionType.ProductCreate.ToString(),
            EntityType = EntityType.Product.ToString(),
            EntityId = product.Id.ToString(),
            OldValues = null,
            NewValues = JsonSerializer.Serialize(new
            {
                id = product.Id,
                name = product.Name,
                price = product.Price,
                category = product.Category,
                stockQuantity = product.StockQuantity,
                reorderLevel = product.ReorderLevel,
                isDeleted = product.IsDeleted
            }),
            Timestamp = now,
            IpAddress = ipAddress,
        });

        await _db.SaveChangesAsync(cancellationToken);

        var body = new AdminProductCreateResponseDto(product.Id, product.Name, product.Price, product.Category, product.StockQuantity, product.CreatedAt);
        return (true, product.Id, body, null);
    }

    public async Task<(bool Success, object? Body, object? ErrorBody)> UpdateAsync(Guid adminUserId, int productId, AdminProductUpsertRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
        if (product is null)
        {
            return (false, null, new { error = "Product not found", statusCode = 404 });
        }

        var oldValues = JsonSerializer.Serialize(new
        {
            name = product.Name,
            description = product.Description,
            price = product.Price,
            category = product.Category,
            stockQuantity = product.StockQuantity,
            reorderLevel = product.ReorderLevel,
            specifications = product.Specifications,
            imageUrl = product.ImageUrl,
            isDeleted = product.IsDeleted,
        });

        product.Name = request.Name.Trim();
        product.Category = request.Category.Trim();
        product.Description = request.Description;
        product.Price = request.Price;
        product.StockQuantity = request.StockQuantity;
        product.ReorderLevel = request.ReorderLevel ?? product.ReorderLevel;
        product.Specifications = request.Specifications is null ? null : JsonSerializer.Serialize(request.Specifications);
        product.ImageUrl = request.ImageUrl;
        product.UpdatedAt = now;

        var newValues = JsonSerializer.Serialize(new
        {
            name = product.Name,
            description = product.Description,
            price = product.Price,
            category = product.Category,
            stockQuantity = product.StockQuantity,
            reorderLevel = product.ReorderLevel,
            specifications = product.Specifications,
            imageUrl = product.ImageUrl,
            isDeleted = product.IsDeleted,
        });

        _db.AdminActions.Add(new AdminAction
        {
            AdminUserId = adminUserId,
            ActionType = AdminActionType.ProductUpdate.ToString(),
            EntityType = EntityType.Product.ToString(),
            EntityId = product.Id.ToString(),
            OldValues = oldValues,
            NewValues = newValues,
            Timestamp = now,
            IpAddress = ipAddress,
        });

        await _db.SaveChangesAsync(cancellationToken);

        return (true, new { success = true, productId = product.Id, updatedAt = product.UpdatedAt }, null);
    }

    public async Task<(bool Success, object? ErrorBody)> SoftDeleteAsync(Guid adminUserId, int productId, string? ipAddress, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
        if (product is null)
        {
            return (false, new { error = "Product not found", statusCode = 404 });
        }

        if (product.IsDeleted)
        {
            return (true, null);
        }

        var oldValues = JsonSerializer.Serialize(new { isDeleted = false });
        var newValues = JsonSerializer.Serialize(new { isDeleted = true });

        product.IsDeleted = true;
        product.UpdatedAt = now;

        _db.AdminActions.Add(new AdminAction
        {
            AdminUserId = adminUserId,
            ActionType = AdminActionType.ProductDelete.ToString(),
            EntityType = EntityType.Product.ToString(),
            EntityId = product.Id.ToString(),
            OldValues = oldValues,
            NewValues = newValues,
            Timestamp = now,
            IpAddress = ipAddress,
        });

        await _db.SaveChangesAsync(cancellationToken);

        return (true, null);
    }
}
