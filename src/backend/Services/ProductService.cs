using Microsoft.EntityFrameworkCore;
using ReactCore.Backend.Models.Dto;
using ReactCore.Backend.Repositories;
using System.Text.Json;

namespace ReactCore.Backend.Services;

public class ProductService : IProductService
{
    private const int MaxTake = 100;

    private readonly IProductRepository _productRepository;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IProductRepository productRepository, ILogger<ProductService> logger)
    {
        _productRepository = productRepository;
        _logger = logger;
    }

    public async Task<ProductListDto> GetProductsAsync(
        string? category,
        decimal? minPrice,
        decimal? maxPrice,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        if (skip < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(skip), "skip must be >= 0");
        }

        if (take <= 0 || take > MaxTake)
        {
            throw new ArgumentOutOfRangeException(nameof(take), $"take must be between 1 and {MaxTake}");
        }

        if (minPrice.HasValue && minPrice.Value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minPrice), "minPrice must be >= 0");
        }

        if (maxPrice.HasValue && maxPrice.Value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxPrice), "maxPrice must be >= 0");
        }

        if (minPrice.HasValue && maxPrice.HasValue && minPrice.Value > maxPrice.Value)
        {
            throw new ArgumentException("minPrice must be <= maxPrice");
        }

        var query = _productRepository.QueryActive();

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(p => p.Category == category);
        }

        if (minPrice.HasValue)
        {
            query = query.Where(p => p.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= maxPrice.Value);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.Id)
            .Skip(skip)
            .Take(take)
            .Select(p => new ProductListItemDto(
                p.Id,
                p.Name,
                p.Description,
                p.Price,
                p.Category,
                p.ImageUrl,
                p.StockQuantity,
                p.ReorderLevel,
                GetStatus(p.StockQuantity, p.ReorderLevel)))
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Listed products: total={Total} skip={Skip} take={Take} category={Category} minPrice={MinPrice} maxPrice={MaxPrice}",
            total,
            skip,
            take,
            string.IsNullOrWhiteSpace(category) ? "(none)" : category,
            minPrice,
            maxPrice);

        return new ProductListDto(items, total, skip, take);
    }

    private static string GetStatus(int stockQuantity, int reorderLevel)
    {
        if (stockQuantity <= 0) return "out-of-stock";
        if (stockQuantity <= reorderLevel) return "low-stock";
        return "in-stock";
    }

    public async Task<ProductDetailDto?> GetProductByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "id must be >= 1");
        }

        var product = await _productRepository.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return null;
        }

        var imageUrls = new List<string>();
        if (!string.IsNullOrWhiteSpace(product.ImageUrl))
        {
            imageUrls.Add(product.ImageUrl);
        }

        imageUrls.AddRange(
            product.Images
                .OrderBy(i => i.DisplayOrder)
                .Select(i => i.ImageUrl)
                .Where(u => !string.IsNullOrWhiteSpace(u)));

        var distinctImageUrls = imageUrls
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var specifications = ParseSpecifications(product.Specifications);

        return new ProductDetailDto(
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.Category,
            product.ImageUrl,
            distinctImageUrls,
            specifications,
            product.StockQuantity,
            product.ReorderLevel,
            GetStatus(product.StockQuantity, product.ReorderLevel),
            Array.Empty<RelatedProductDto>(),
            product.CreatedAt,
            product.UpdatedAt);
    }

    public async Task<IReadOnlyList<RelatedProductDto>> GetRelatedProductsAsync(
        int productId,
        int count,
        CancellationToken cancellationToken = default)
    {
        if (productId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(productId), "productId must be >= 1");
        }

        if (count <= 0 || count > 20)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "count must be between 1 and 20");
        }

        var baseProduct = await _productRepository.GetByIdAsync(productId, cancellationToken);
        if (baseProduct is null)
        {
            return Array.Empty<RelatedProductDto>();
        }

        return await _productRepository
            .QueryActive()
            .Where(p => p.Id != productId && p.Category == baseProduct.Category)
            .OrderBy(_ => Guid.NewGuid())
            .Take(count)
            .Select(p => new RelatedProductDto(p.Id, p.Name, p.Price, p.ImageUrl, p.Category))
            .ToListAsync(cancellationToken);
    }

    private static IReadOnlyDictionary<string, object?> ParseSpecifications(string? specificationsJson)
    {
        if (string.IsNullOrWhiteSpace(specificationsJson))
        {
            return new Dictionary<string, object?>();
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, object?>>(specificationsJson);
            return parsed ?? new Dictionary<string, object?>();
        }
        catch
        {
            return new Dictionary<string, object?>();
        }
    }
}
