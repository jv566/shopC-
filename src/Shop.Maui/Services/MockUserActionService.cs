using Shop.Maui.Models;

namespace Shop.Maui.Services;

public sealed class MockUserActionService : IUserActionService
{
    private readonly List<ProductListItem> _cartItems = [];
    private readonly List<string> _myOrders =
    [
        "SO-20260401-001",
        "SO-20260408-003"
    ];
    private readonly List<string> _historyOrders =
    [
        "HO-20250316-002",
        "HO-20250212-007",
        "HO-20250125-005"
    ];
    private int _syncCount;

    public Task<UserActionResult> AddToCartAsync(ProductListItem product, CancellationToken cancellationToken = default)
    {
        _cartItems.Add(product);

        return Task.FromResult(new UserActionResult(
            "购物车",
            $"{product.ModelName} 已加入购物车，当前共 {_cartItems.Count} 件商品。"));
    }

    public Task<UserActionResult> BuyNowAsync(ProductListItem product, CancellationToken cancellationToken = default)
    {
        var orderNo = $"SO-{DateTime.Now:yyyyMMdd}-{_myOrders.Count + 1:000}";
        _myOrders.Insert(0, orderNo);

        return Task.FromResult(new UserActionResult(
            "立即购买",
            $"已创建订单 {orderNo}，商品：{product.ModelName}，金额：￥{product.SalePrice:F2}。"));
    }

    public Task<UserActionResult> GetCartSummaryAsync(CancellationToken cancellationToken = default)
    {
        var total = _cartItems.Sum(x => x.SalePrice);
        var message = _cartItems.Count == 0
            ? "购物车当前为空，接口已接通。"
            : $"购物车共有 {_cartItems.Count} 件商品，合计 ￥{total:F2}。";

        return Task.FromResult(new UserActionResult("购物车", message));
    }

    public Task<UserActionResult> GetMyOrdersSummaryAsync(CancellationToken cancellationToken = default)
    {
        var latest = _myOrders.FirstOrDefault() ?? "暂无订单";
        return Task.FromResult(new UserActionResult(
            "我的订单",
            $"当前订单数：{_myOrders.Count}，最近订单：{latest}。"));
    }

    public Task<UserActionResult> GetHistoryOrdersSummaryAsync(CancellationToken cancellationToken = default)
    {
        var latest = _historyOrders.FirstOrDefault() ?? "暂无历史订单";
        return Task.FromResult(new UserActionResult(
            "历史订单",
            $"历史订单数：{_historyOrders.Count}，最近一笔：{latest}。"));
    }

    public Task<UserActionResult> SyncQrAsync(CancellationToken cancellationToken = default)
    {
        _syncCount++;
        return Task.FromResult(new UserActionResult(
            "二维码同步",
            $"二维码同步完成，第 {_syncCount} 次请求已返回成功。"));
    }
}
