using ReactCore.Backend.Models.Dto;

namespace ReactCore.Backend.Services;

public interface INotificationService
{
    Task SubscribeToRestockAsync(Guid userId, int productId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TriggeredNotificationDto>> CheckPendingNotificationsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

public record TriggeredNotificationDto(
    int ProductId,
    string ProductName,
    int StockQuantity,
    int ReorderLevel,
    string Status,
    DateTime TriggeredAt
);
