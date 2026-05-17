using System.Net.Http.Json;
using System.Globalization;
using System.Text.Json;
using Shop.Maui.Models;

namespace Shop.Maui.Services;

public sealed class MockUserActionService : IUserActionService
{
    private const string AddOrderUnitId = "2";
    private const string CheckoutUnitId = "1001";
    private const string DefaultOpId = "1200";
    private const string DefaultUserId = "0";
    private const string DefaultPayNote = "微信支付-自提";
    private const string DefaultPayChannel = "SQB";
    private const string DefaultPayReturnUrl = "/subPackages/package/pages/jiesuan-payResult/jiesuan-payResult";
    private const string DefaultAddress = "天河路店";

    private static readonly JsonSerializerOptions OrderJsonOptions = new()
    {
        PropertyNamingPolicy = null
    };

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
            $"https://www.ruanzi.net/jy/go/phone.aspx?mbid=10627&ituid=121&itsid={itsId}",
            cancellationToken);
        if (!createResult.Succeeded)
        {
            return new UserActionResult("创建订单失败", createResult.Message);
        }

        foreach (var item in lines)
        {
            var addPayload = new
            {
                MCODE = item.ProductId,
                NUM = item.Quantity,
                UNITID = AddOrderUnitId,
                add = BuildOrderLineType(item),
                img = BuildOrderLineImage(item)
            };
            var addUrl = $"https://www.ruanzi.net/jy/go/phone.aspx?mbid=10604&ituid=121&itsid={itsId}";
            var addResult = await PostOrderStepAsync(
                addUrl,
                addPayload,
                cancellationToken);

            if (!addResult.Succeeded)
            {
                return new UserActionResult(
                    "添加商品失败",
                    $"{item.ModelName}: {addResult.Message}\nURL: {addUrl}\nBody: {JsonSerializer.Serialize(addPayload, OrderJsonOptions)}");
            }
        }

        var totalAmount = lines.Sum(item => item.Subtotal);
        var payResult = await PostOrderStepAsync(
            $"https://www.ruanzi.net/jy/go/phone.aspx?mbid=122&ituid=121&itsid={itsId}",
            new
            {
                MCODE = string.Empty,
                OPID = DefaultOpId,
                UNITID = CheckoutUnitId,
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

    public async Task<IReadOnlyList<OrderListItem>> GetMyOrdersAsync(CancellationToken cancellationToken = default)
    {
        return await FetchOrdersAsync(_myOrders, cancellationToken);
    }

    public async Task<IReadOnlyList<OrderListItem>> GetHistoryOrdersAsync(CancellationToken cancellationToken = default)
    {
        return await FetchOrdersAsync(_historyOrders, cancellationToken);
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
                    product.CategoryId,
                    product.ProductType,
                    product.ModelName,
                    product.SalePrice,
                    group.Count(),
                    product.ImageUrl);
            })
            .ToList();
    }

    private async Task<IReadOnlyList<OrderListItem>> FetchOrdersAsync(
        IReadOnlyList<OrderListItem> fallbackOrders,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_authSession.ItsId))
        {
            return fallbackOrders.ToList();
        }

        try
        {
            var requestUrl =
                $"https://www.ruanzi.net/jy/go/we.aspx?ituid=121&itjid=12107&itcid=12107&itsid={Uri.EscapeDataString(_authSession.ItsId)}";

            using var response = await _httpClient.GetAsync(requestUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return fallbackOrders.ToList();
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var json = ExtractJsonObject(body);
            if (string.IsNullOrWhiteSpace(json))
            {
                return fallbackOrders.ToList();
            }

            using var document = JsonDocument.Parse(json);
            if (!TryFindProperty(document.RootElement, "goods", out var goods) ||
                goods.ValueKind != JsonValueKind.Array)
            {
                return fallbackOrders.ToList();
            }

            var orders = goods
                .EnumerateArray()
                .Select(ParseOrder)
                .OfType<OrderListItem>()
                .ToList();

            return orders.Count == 0 ? Array.Empty<OrderListItem>() : orders;
        }
        catch
        {
            return fallbackOrders.ToList();
        }
    }

    private static OrderListItem? ParseOrder(JsonElement item)
    {
        var orderNo = FirstNonEmpty(
            GetString(item, "orderNo"),
            GetString(item, "orderno"),
            GetString(item, "orderNumber"),
            GetString(item, "orderid"),
            GetString(item, "id"),
            GetString(item, "code"),
            GetString(item, "NO"));

        if (string.IsNullOrWhiteSpace(orderNo))
        {
            orderNo = $"ORDER-{DateTime.Now:yyyyMMddHHmmss}";
        }

        var status = FirstNonEmpty(
            GetString(item, "status"),
            GetString(item, "state"),
            GetString(item, "zt"),
            GetString(item, "ZT"),
            "已下单");

        var createdAt = ParseDate(FirstNonEmpty(
            GetString(item, "createdAt"),
            GetString(item, "createTime"),
            GetString(item, "time"),
            GetString(item, "date"),
            GetString(item, "rq"),
            GetString(item, "RQ")));

        var productName = FirstNonEmpty(
            GetString(item, "name"),
            GetString(item, "productName"),
            GetString(item, "goodsName"),
            GetString(item, "mname"),
            GetString(item, "MNAME"),
            GetString(item, "MCODE"),
            "商品");

        var quantity = ParseInt(FirstNonEmpty(
            GetString(item, "num"),
            GetString(item, "NUM"),
            GetString(item, "quantity"),
            GetString(item, "sl"),
            GetString(item, "SL")), 1);

        var unitPrice = ParseDecimal(FirstNonEmpty(
            GetString(item, "price"),
            GetString(item, "PRICE"),
            GetString(item, "unitPrice"),
            GetString(item, "AMT"),
            GetString(item, "amt")), 0m);

        return new OrderListItem(
            orderNo,
            status,
            createdAt,
            [new OrderLineItem(productName, quantity, unitPrice)]);
    }

    private static string NormalizeOrderImage(string? imageUrl)
    {
        var value = (imageUrl ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value;
    }

    private static string BuildOrderLineType(CartLineItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.ProductType))
        {
            return Uri.UnescapeDataString(item.ProductType.Trim());
        }

        if (!string.IsNullOrWhiteSpace(item.CategoryId))
        {
            return Uri.UnescapeDataString(item.CategoryId.Trim());
        }

        return Uri.UnescapeDataString(item.ModelName.Trim());
    }

    private static string BuildOrderLineImage(CartLineItem item)
    {
        return "images/coffee.png";
    }

    private async Task<OrderStepResult> PostOrderStepAsync(
        string url,
        object payload,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                url,
                payload,
                OrderJsonOptions,
                cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new OrderStepResult(false, ExtractMessage(body, $"请求失败：{response.StatusCode}"), body);
            }

            return new OrderStepResult(true, ExtractMessage(body, "操作成功"), body);
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

            return new OrderStepResult(true, ExtractMessage(body, "操作成功"), body);
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
            foreach (var name in new[] { "msg", "message", "desc", "error", "info" })
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

    private static string ExtractJsonObject(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return string.Empty;
        }

        var text = body.Trim();
        if (text.StartsWith('{') && text.EndsWith('}'))
        {
            return text;
        }

        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        return start >= 0 && end > start
            ? text[start..(end + 1)]
            : string.Empty;
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        return TryFindProperty(element, propertyName, out var property)
            ? property.ToString()
            : string.Empty;
    }

    private static string FirstNonEmpty(params string[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;
    }

    private static DateTime ParseDate(string value)
    {
        return DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out var date)
            ? date
            : DateTime.Now;
    }

    private static int ParseInt(string value, int fallback)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) && number > 0
            ? number
            : fallback;
    }

    private static decimal ParseDecimal(string value, decimal fallback)
    {
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number)
            ? number
            : fallback;
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
