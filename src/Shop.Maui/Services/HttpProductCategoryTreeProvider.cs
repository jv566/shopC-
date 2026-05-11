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

    private readonly object _cacheLock = new();
    private readonly HttpClient _httpClient = new();

    private IReadOnlyList<ProductCategoryGroup> _cachedCategoryTree =
        ProductCategoryCatalog.GetCategoryTree();

    private Task? _refreshTask;

    public async Task<IReadOnlyList<ProductCategoryGroup>> GetCategoryTreeAsync(
        CancellationToken cancellationToken = default)
    {
        StartRefreshIfNeeded();

        await Task.Yield();

        lock (_cacheLock)
        {
            return _cachedCategoryTree;
        }
    }

    private void StartRefreshIfNeeded()
    {
        lock (_cacheLock)
        {
            if (_refreshTask is not null && !_refreshTask.IsCompleted)
            {
                return;
            }

            _refreshTask = RefreshCacheAsync();
        }
    }

    private async Task RefreshCacheAsync()
    {
        try
        {
            using var response = await _httpClient.GetAsync(
                CategoryTreeUrl,
                CancellationToken.None);
            response.EnsureSuccessStatusCode();

            await using var stream =
                await response.Content.ReadAsStreamAsync(CancellationToken.None);

            var payload = await JsonSerializer.DeserializeAsync<CategoryTreeResponse>(
                stream,
                SerializerOptions,
                CancellationToken.None);

            var groups = payload?.Result?.Goods?
                .Select(BuildGroup)
                .OfType<ProductCategoryGroup>()
                .ToList();

            if (groups is not { Count: > 0 })
            {
                return;
            }

            lock (_cacheLock)
            {
                _cachedCategoryTree = groups;
            }
        }
        catch
        {
        }
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
