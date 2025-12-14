using ReactCore.Backend.Models.Dto;

namespace ReactCore.Backend.Services;

public interface IProductService
{
    Task<ProductListDto> GetProductsAsync(
        string? category,
        decimal? minPrice,
        decimal? maxPrice,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<ProductDetailDto?> GetProductByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RelatedProductDto>> GetRelatedProductsAsync(
        int productId,
        int count,
        CancellationToken cancellationToken = default);
}
