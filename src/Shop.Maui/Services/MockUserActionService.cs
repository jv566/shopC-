using System.Net.Http.Json;
using System.Text.Json;
using Shop.Maui.Models;

namespace Shop.Maui.Services;

public sealed class MockUserActionService : IUserActionService
{
    private const string DefaultUnitId = "1001";
    private const string DefaultOpId = "1200";
    private const string DefaultUserId = "0";
    private const string DefaultPayNote = "微信支付-自提";
    private const string DefaultPayChannel = "SQB";
    private const string DefaultPayReturnUrl = "/subPackages/package/pages/jiesuan-payResult/jiesuan-payResult";
    private const string DefaultAddress = "天河路店";

    private readonly IAuthSession _authSession;
    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(12)
    };

    private readonly List<ProductListItem> _cartItems = [];
    private readonly List<OrderListItem> _myOrders = [];
    private readonly List<OrderListItem> _historyOrders = [];
    private int _syncCount;

    public MockUserActionService(IAuthSession authSession)
    {
        _authSession = authSession;
    }

    public Task<UserActionResult> AddToCartAsync(ProductListItem product, CancellationToken cancellationToken = default)
    {
        _cartItems.Add(product);

        return Task.FromResult(new UserActionResult(
            "购物车",
            $"{product.ModelName} 已加入购物车，当前共 {_cartItems.Count} 件商品。"));
    }

    public async Task<UserActionResult> BuyNowAsync(ProductListItem product, CancellationToken cancellationToken = default)
    {
        _cartItems.Add(product);
        return await CheckoutCartAsync(cancellationToken);
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

    public async Task<UserActionResult> CheckoutCartAsync(CancellationToken cancellationToken = default)
    {
        var lines = BuildCartLineItems();
        if (lines.Count == 0)
        {
            return new UserActionResult("购物车", "购物车为空，无法购买。");
        }

        if (string.IsNullOrWhiteSpace(_authSession.ItsId))
        {
            return new UserActionResult("购买失败", "登录状态已失效，请重新登录后再购买。");
        }

        var itsId = Uri.EscapeDataString(_authSession.ItsId);

        // 创建订单三步接口依赖登录返回的 itsid：
        // 1. 创建订单；2. 将购物车商品逐个加入订单；3. 发起结算/付款。
        var createResult = await GetOrderStepAsync(
            $"https://www.zyyai.com.cn/jy/go/phone.aspx?mbid=10627&ituid=106&itsid={itsId}",
            cancellationToken);
        if (!createResult.Succeeded)
        {
            return new UserActionResult("创建订单失败", createResult.Message);
        }

        foreach (var item in lines)
        {
            var addResult = await PostOrderStepAsync(
                $"https://www.zyyai.com.cn/jy/go/phone.aspx?mbid=10604&ituid=106&itsid={itsId}",
                new
                {
                    MCODE = item.ProductId,
                    NUM = item.Quantity,
                    UNITID = DefaultUnitId,
                    add = item.ModelName,
                    img = item.ImageUrl ?? string.Empty
                },
                cancellationToken);

            if (!addResult.Succeeded)
            {
                return new UserActionResult("添加商品失败", $"{item.ModelName}：{addResult.Message}");
            }
        }

        var totalAmount = lines.Sum(item => item.Subtotal);
        var payResult = await PostOrderStepAsync(
            $"https://www.zyyai.com.cn/jy/go/phone.aspx?mbid=122&ituid=106&itsid={itsId}",
            new
            {
                MCODE = string.Empty,
                OPID = DefaultOpId,
                UNITID = DefaultUnitId,
                NUM = string.Empty,
                USERID = DefaultUserId,
                NOTE = DefaultPayNote,
                AMT = totalAmount,
                XXSQ = DefaultPayChannel,
                RURL = DefaultPayReturnUrl,
                type = 1,
                username = _authSession.Phone ?? string.Empty,
                phone = _authSession.Phone ?? string.Empty,
                address = DefaultAddress,
                extra = "{\"in_lite_app\":true}"
            },
            cancellationToken);

        if (!payResult.Succeeded)
        {
            return new UserActionResult("结算失败", payResult.Message);
        }

        var orderNo = ExtractOrderNo(createResult.Body) ?? $"SO-{DateTime.Now:yyyyMMdd}-{_myOrders.Count + 1:000}";
        _myOrders.Insert(
            0,
            new OrderListItem(
                orderNo,
                "待确认",
                DateTime.Now,
                lines.Select(item => new OrderLineItem(item.ModelName, item.Quantity, item.UnitPrice)).ToList()));

        _cartItems.Clear();

        return new UserActionResult("购买成功", $"订单已提交，合计 ￥{totalAmount:F2}。");
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

    private async Task<OrderStepResult> PostOrderStepAsync(
        string url,
        object payload,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new OrderStepResult(false, ExtractMessage(body, $"请求失败：{response.StatusCode}"), body);
            }

            return LooksSuccessful(body)
                ? new OrderStepResult(true, ExtractMessage(body, "操作成功"), body)
                : new OrderStepResult(false, ExtractMessage(body, "操作失败，请稍后重试。"), body);
        }
        catch (Exception ex)
        {
            return new OrderStepResult(false, $"网络请求失败：{ex.Message}", string.Empty);
        }
    }

    private async Task<OrderStepResult> GetOrderStepAsync(
        string url,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new OrderStepResult(false, ExtractMessage(body, $"请求失败：{response.StatusCode}"), body);
            }

            return LooksSuccessful(body)
                ? new OrderStepResult(true, ExtractMessage(body, "操作成功"), body)
                : new OrderStepResult(false, ExtractMessage(body, "操作失败，请稍后重试。"), body);
        }
        catch (Exception ex)
        {
            return new OrderStepResult(false, $"网络请求失败：{ex.Message}", string.Empty);
        }
    }

    private static bool LooksSuccessful(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return true;
        }

        var text = body.Trim();
        if (text.Contains("失败", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("错误", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("error", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("\"success\":false", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (text.Contains("成功", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("\"success\":true", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        try
        {
            using var document = JsonDocument.Parse(text);
            foreach (var name in new[] { "code", "status", "state", "result" })
            {
                if (!TryFindProperty(document.RootElement, name, out var property))
                {
                    continue;
                }

                if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var number))
                {
                    return number is 0 or 1 or 200;
                }

                if (property.ValueKind == JsonValueKind.String)
                {
                    var value = property.GetString();
                    return value is "0" or "1" or "200" or "success" or "ok";
                }
            }
        }
        catch
        {
        }

        return true;
    }

    private static string ExtractMessage(string body, string fallback)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return fallback;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            foreach (var name in new[] { "msg", "message", "error", "info" })
            {
                if (TryFindProperty(document.RootElement, name, out var property) &&
                    property.ValueKind == JsonValueKind.String &&
                    !string.IsNullOrWhiteSpace(property.GetString()))
                {
                    return property.GetString()!;
                }
            }
        }
        catch
        {
        }

        return body.Length > 80 ? fallback : body;
    }

    private static string? ExtractOrderNo(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            foreach (var name in new[] { "orderNo", "orderno", "orderNumber", "orderid", "id" })
            {
                if (TryFindProperty(document.RootElement, name, out var property))
                {
                    var value = property.ToString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value;
                    }
                }
            }
        }
        catch
        {
        }

        return null;
    }

    private static bool TryFindProperty(JsonElement element, string propertyName, out JsonElement property)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var item in element.EnumerateObject())
            {
                if (string.Equals(item.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    property = item.Value;
                    return true;
                }

                if (TryFindProperty(item.Value, propertyName, out property))
                {
                    return true;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                if (TryFindProperty(item, propertyName, out property))
                {
                    return true;
                }
            }
        }

        property = default;
        return false;
    }

    private sealed record OrderStepResult(bool Succeeded, string Message, string Body);
}
