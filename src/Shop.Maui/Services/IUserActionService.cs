using Shop.Maui.Models;

namespace Shop.Maui.Services;

public interface IUserActionService
{
    Task<UserActionResult> AddToCartAsync(ProductListItem product, CancellationToken cancellationToken = default);

    Task<UserActionResult> BuyNowAsync(ProductListItem product, CancellationToken cancellationToken = default);

    Task<UserActionResult> GetCartSummaryAsync(CancellationToken cancellationToken = default);

    Task<UserActionResult> GetMyOrdersSummaryAsync(CancellationToken cancellationToken = default);

    Task<UserActionResult> GetHistoryOrdersSummaryAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CartLineItem>> GetCartItemsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderListItem>> GetMyOrdersAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderListItem>> GetHistoryOrdersAsync(CancellationToken cancellationToken = default);

    Task<UserActionResult> SyncQrAsync(CancellationToken cancellationToken = default);
}
