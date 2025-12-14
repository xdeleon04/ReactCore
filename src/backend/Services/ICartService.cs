using ReactCore.Backend.Models.Dto;

namespace ReactCore.Backend.Services;

public interface ICartService
{
    Task<CartDto> GetCartAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<AddToCartResponse> AddItemAsync(Guid userId, AddToCartRequest request, CancellationToken cancellationToken = default);
    Task<UpdateCartItemResponse> UpdateItemAsync(Guid userId, int itemId, UpdateCartItemRequest request, CancellationToken cancellationToken = default);
    Task RemoveItemAsync(Guid userId, int itemId, CancellationToken cancellationToken = default);
    Task ClearCartAsync(Guid userId, CancellationToken cancellationToken = default);
}
