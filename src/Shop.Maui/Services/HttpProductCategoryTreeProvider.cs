using System.Text.Json;               // 用来把 JSON 字符串/流反序列化成 C# 对象
using System.Text.Json.Serialization; // 用来设置 JSON 字段名映射，比如 JsonPropertyName
using Shop.Maui.Models;               // 引入项目里的分类模型

namespace Shop.Maui.Services;

// 通过 HTTP 接口获取商品分类树的服务类
// 实现 IProductCategoryTreeProvider，说明它具备“获取分类树”的能力
public sealed class HttpProductCategoryTreeProvider : IProductCategoryTreeProvider
{
    // 分类树接口地址
    // 程序会请求这个地址，拿到一级分类和二级分类数据
    private const string CategoryTreeUrl =
        "http://www.ruanzi.net/jy/go/we.aspx?ituid=121&itjid=12102&itcid=12102";

    // JSON 解析配置
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        // 属性名大小写不敏感
        // 比如 JSON 里是 "code"，C# 属性是 Code，也能匹配
        PropertyNameCaseInsensitive = true
    };

    // HttpClient 用来发送 HTTP 请求
    private readonly HttpClient _httpClient = new();

    // 获取分类树
    public async Task<IReadOnlyList<ProductCategoryGroup>> GetCategoryTreeAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 发送 GET 请求
            using var response = await _httpClient.GetAsync(
                CategoryTreeUrl,
                cancellationToken);

            // 如果状态码不是 200-299，会直接抛异常
            // 比如 404、500 都会进入 catch
            response.EnsureSuccessStatusCode();

            // 把响应内容读取成流
            await using var stream =
                await response.Content.ReadAsStreamAsync(cancellationToken);

            // 把 JSON 数据反序列化成 CategoryTreeResponse 对象
            var payload = await JsonSerializer.DeserializeAsync<CategoryTreeResponse>(
                stream,
                SerializerOptions,
                cancellationToken);

            // 从接口返回对象里取 result.goods
            // 然后把每个接口分类对象 CategoryTreeItem
            // 转换成项目内部使用的 ProductCategoryGroup
            var groups = payload?.Result?.Goods?
                .Select(BuildGroup)              // 每个接口分类转成 ProductCategoryGroup?
                .OfType<ProductCategoryGroup>()  // 去掉 null，只保留成功转换的分类组
                .ToList();

            // 如果接口返回的分类组数量大于 0，就使用接口数据
            // 否则使用本地默认分类数据
            return groups is { Count: > 0 }
                ? groups
                : ProductCategoryCatalog.GetCategoryTree();
        }
        catch
        {
            // 如果请求失败、JSON 解析失败、接口异常等
            // 就返回本地默认分类，保证页面不会崩
            return ProductCategoryCatalog.GetCategoryTree();
        }
    }

    // 把接口返回的一个一级分类对象，转换成项目内部的分类组
    private static ProductCategoryGroup? BuildGroup(CategoryTreeItem item)
    {
        // 先把一级分类本身转换成 ProductCategoryOption
        var primary = BuildOption(item);

        // 如果一级分类 id 或 text 不合法，就返回 null
        if (primary is null)
        {
            return null;
        }

        // 把 children 里的二级分类也转换成 ProductCategoryOption
        // item.Children ?? [] 表示：
        // 如果 Children 是 null，就使用空数组，避免空引用错误
        var children = (item.Children ?? [])
            .Select(BuildOption)                 // 转换每个二级分类
            .OfType<ProductCategoryOption>()     // 去掉 null
            .ToList();

        // 组合成一个分类组：
        // 一级分类 + 二级分类列表
        return new ProductCategoryGroup(primary, children);
    }

    // 把接口里的分类对象转换成项目内部的 ProductCategoryOption
    private static ProductCategoryOption? BuildOption(CategoryTreeItem item)
    {
        // 如果接口返回的 id 或 text 为空，就认为这个分类无效
        if (string.IsNullOrWhiteSpace(item.Id) ||
            string.IsNullOrWhiteSpace(item.Text))
        {
            return null;
        }

        // Trim() 去掉前后空格
        // item.Id.Trim() 作为分类 id
        // item.Text.Trim() 作为分类显示名称
        return new ProductCategoryOption(
            item.Id.Trim(),
            item.Text.Trim());
    }

    // 下面这几个 private sealed class 是专门用来接收接口 JSON 的
    // 它们不是页面真正使用的模型，只是“接口返回数据的临时模型”

    // 对应接口返回的最外层 JSON
    private sealed class CategoryTreeResponse
    {
        // 对应 JSON 字段 "code"
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        // 对应 JSON 字段 "msg"
        [JsonPropertyName("msg")]
        public string? Message { get; set; }

        // 对应 JSON 字段 "result"
        [JsonPropertyName("result")]
        public CategoryTreeResult? Result { get; set; }
    }

    // 对应 JSON 里的 result 对象
    private sealed class CategoryTreeResult
    {
        // 对应 JSON 字段 "goods"
        // goods 里面是一组分类
        [JsonPropertyName("goods")]
        public List<CategoryTreeItem>? Goods { get; set; }
    }

    // 对应每一个分类节点
    // 一级分类和二级分类都可以用这个类表示
    private sealed class CategoryTreeItem
    {
        // 分类 id
        // 对应 JSON 字段 "id"
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        // 分类名称
        // 对应 JSON 字段 "text"
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        // 子分类
        // 一级分类下面的二级分类会放在 children 里
        [JsonPropertyName("children")]
        public List<CategoryTreeItem>? Children { get; set; }
    }
}