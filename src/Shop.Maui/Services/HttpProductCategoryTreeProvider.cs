using System.Text.Json;
using System.Text.Json.Serialization;
using Shop.Maui.Models;

namespace Shop.Maui.Services;

public sealed class HttpProductCategoryTreeProvider : IProductCategoryTreeProvider
{
    private const string CategoryTreeUrl =
        "https://www.ruanzi.net/jy/go/we.aspx?ituid=121&itjid=12102&itcid=12102";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient = new();
    private readonly object _cacheLock = new();
    private IReadOnlyList<ProductCategoryGroup>? _cachedCategoryTree;

    public async Task<IReadOnlyList<ProductCategoryGroup>> GetCategoryTreeAsync(
        CancellationToken cancellationToken = default)
    {
        lock (_cacheLock)
        {
            if (_cachedCategoryTree is not null)
            {
                return _cachedCategoryTree;
            }
        }

        try
        {
            using var response = await _httpClient.GetAsync(
                CategoryTreeUrl,
                cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream =
                await response.Content.ReadAsStreamAsync(cancellationToken);

            var payload = await JsonSerializer.DeserializeAsync<CategoryTreeResponse>(
                stream,
                SerializerOptions,
                cancellationToken);

            var groups = payload?.Result?.Goods?
                .Select(BuildGroup)
                .OfType<ProductCategoryGroup>()
                .ToList();

            if (groups is { Count: > 0 })
            {
                lock (_cacheLock)
                {
                    _cachedCategoryTree = groups;
                }

                return groups;
            }
        }
        catch
        {
        }

        return ProductCategoryCatalog.GetCategoryTree();
    }

    private static ProductCategoryGroup? BuildGroup(CategoryTreeItem item)
    {
        var primary = BuildOption(item);
        if (primary is null)
        {
            return null;
        }

        var children = (item.Children ?? [])
            .Select(BuildOption)
            .OfType<ProductCategoryOption>()
            .ToList();

        return new ProductCategoryGroup(primary, children);
    }

    private static ProductCategoryOption? BuildOption(CategoryTreeItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Id) ||
            string.IsNullOrWhiteSpace(item.Text))
        {
            return null;
        }

        return new ProductCategoryOption(
            item.Id.Trim(),
            item.Text.Trim());
    }

    private sealed class CategoryTreeResponse
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("msg")]
        public string? Message { get; set; }

        [JsonPropertyName("result")]
        public CategoryTreeResult? Result { get; set; }
    }

    private sealed class CategoryTreeResult
    {
        [JsonPropertyName("goods")]
        public List<CategoryTreeItem>? Goods { get; set; }
    }

    private sealed class CategoryTreeItem
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("children")]
        public List<CategoryTreeItem>? Children { get; set; }
    }
}
