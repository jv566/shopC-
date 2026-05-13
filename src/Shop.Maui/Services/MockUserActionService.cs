using Shop.Maui.Models;

namespace Shop.Maui.Services;

public sealed class MockUserActionService : IUserActionService
{
    private readonly List<ProductListItem> _cartItems = [];
    private readonly List<OrderListItem> _myOrders =
    [
        new(
            "SO-20260408-003",
            "待确认",
            new DateTime(2026, 4, 8, 14, 30, 0),
            [
                new OrderLineItem("现代布艺沙发", 1, 3299m),
                new OrderLineItem("岩板茶几", 1, 1280m)
            ]),
        new(
            "SO-20260401-001",
            "配送中",
            new DateTime(2026, 4, 1, 10, 15, 0),
            [
                new OrderLineItem("实木餐桌", 1, 4599m),
                new OrderLineItem("餐椅组合", 4, 399m)
            ])
    ];
    private readonly List<OrderListItem> _historyOrders =
    [
        new(
            "HO-20250316-002",
            "已完成",
            new DateTime(2025, 3, 16, 16, 45, 0),
            [
                new OrderLineItem("北欧双人床", 1, 5200m)
            ]),
        new(
            "HO-20250212-007",
            "已完成",
            new DateTime(2025, 2, 12, 9, 5, 0),
            [
                new OrderLineItem("书桌", 1, 1890m),
                new OrderLineItem("书柜", 1, 2390m)
            ]),
        new(
            "HO-20250125-005",
            "已取消",
            new DateTime(2025, 1, 25, 18, 20, 0),
            [
                new OrderLineItem("软包床垫", 1, 2990m)
            ])
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
        _myOrders.Insert(
            0,
            new OrderListItem(
                orderNo,
                "待确认",
                DateTime.Now,
                [new OrderLineItem(product.ModelName, 1, product.SalePrice)]));

        return Task.FromResult(new UserActionResult(
            "立即购买",
            $"已创建订单 {orderNo}，商品：{product.ModelName}，金额：￥{product.SalePrice:F2}。"));
    }

    public Task<UserActionResult> GetCartSummaryAsync(CancellationToken cancellationToken = default)
    {
        var total = _cartItems.Sum(x => x.SalePrice);
        var message = _cartItems.Count == 0
            ? "购物车当前为空。"
            : $"购物车共有 {_cartItems.Count} 件商品，合计 ￥{total:F2}。";

        return Task.FromResult(new UserActionResult("购物车", message));
    }

    public Task<UserActionResult> GetMyOrdersSummaryAsync(CancellationToken cancellationToken = default)
    {
        var latest = _myOrders.FirstOrDefault()?.OrderNo ?? "暂无订单";
        return Task.FromResult(new UserActionResult(
            "我的订单",
            $"当前订单数：{_myOrders.Count}，最近订单：{latest}。"));
    }

    public Task<UserActionResult> GetHistoryOrdersSummaryAsync(CancellationToken cancellationToken = default)
    {
        var latest = _historyOrders.FirstOrDefault()?.OrderNo ?? "暂无历史订单";
        return Task.FromResult(new UserActionResult(
            "历史订单",
            $"历史订单数：{_historyOrders.Count}，最近一笔：{latest}。"));
    }

    public Task<IReadOnlyList<CartLineItem>> GetCartItemsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<CartLineItem>>(BuildCartLineItems());
    }

    public Task<UserActionResult> CheckoutCartAsync(CancellationToken cancellationToken = default)
    {
        var lines = BuildCartLineItems();
        if (lines.Count == 0)
        {
            return Task.FromResult(new UserActionResult("购物车", "购物车为空，无法购买。"));
        }

        var orderNo = $"SO-{DateTime.Now:yyyyMMdd}-{_myOrders.Count + 1:000}";
        _myOrders.Insert(
            0,
            new OrderListItem(
                orderNo,
                "待确认",
                DateTime.Now,
                lines
                    .Select(item => new OrderLineItem(item.ModelName, item.Quantity, item.UnitPrice))
                    .ToList()));

        _cartItems.Clear();

        return Task.FromResult(new UserActionResult(
            "购买成功",
            $"已创建订单 {orderNo}，合计 ￥{lines.Sum(item => item.Subtotal):F2}。"));
    }

    public Task<IReadOnlyList<OrderListItem>> GetMyOrdersAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<OrderListItem>>(_myOrders.ToList());
    }

    public Task<IReadOnlyList<OrderListItem>> GetHistoryOrdersAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<OrderListItem>>(_historyOrders.ToList());
    }

    public Task<UserActionResult> SyncQrAsync(CancellationToken cancellationToken = default)
    {
        _syncCount++;
        return Task.FromResult(new UserActionResult(
            "二维码同步",
            $"二维码同步完成，第 {_syncCount} 次请求已返回成功。"));
    }

    private List<CartLineItem> BuildCartLineItems()
    {
        return _cartItems
            .GroupBy(item => item.Id)
            .Select(group =>
            {
                var product = group.First();
                return new CartLineItem(
                    product.Id,
                    product.ModelName,
                    product.SalePrice,
                    group.Count(),
                    product.ImageUrl);
            })
            .ToList();
    }
}
