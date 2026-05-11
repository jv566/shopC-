using System.Globalization;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Shop.Maui.Models;

namespace Shop.Maui.Services;

public sealed class HttpProductProvider : IProductProvider
{
    private const string ProductListBaseUrl =
        "https://www.ruanzi.net/jy/go/we.aspx?ituid=121&itjid=12101&itcid=12103";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ConcurrentDictionary<string, IReadOnlyList<ProductListItem>> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Lazy<Task<IReadOnlyList<ProductListItem>>>> _inflight = new(StringComparer.OrdinalIgnoreCase);
    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    public async Task<IReadOnlyList<ProductListItem>> GetProductsAsync(
        string categoryId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(categoryId))
        {
            return Array.Empty<ProductListItem>();
        }

        var cacheKey = categoryId.Trim();
        if (_cache.TryGetValue(cacheKey, out var cachedProducts))
        {
            return cachedProducts;
        }

        var lazy = _inflight.GetOrAdd(
            cacheKey,
            key => new Lazy<Task<IReadOnlyList<ProductListItem>>>(
                () => FetchProductsAsync(key, cancellationToken),
                LazyThreadSafetyMode.ExecutionAndPublication));

        try
        {
            var products = await lazy.Value.ConfigureAwait(false);
            if (!cancellationToken.IsCancellationRequested)
            {
                _cache.TryAdd(cacheKey, products);
            }

            return products;
        }
        finally
        {
            _inflight.TryRemove(cacheKey, out _);
        }
    }

    private async Task<IReadOnlyList<ProductListItem>> FetchProductsAsync(
        string categoryId,
        CancellationToken cancellationToken)
    {
        try
        {
            var requestUrl =
                $"{ProductListBaseUrl}&keyvalue={Uri.EscapeDataString(categoryId)}";

            using var response = await _httpClient.GetAsync(requestUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream =
                await response.Content.ReadAsStreamAsync(cancellationToken);

            var payload = await JsonSerializer.DeserializeAsync<ProductListResponse>(
                stream,
                SerializerOptions,
                cancellationToken);

            var products = payload?.Result?.Goods?
                .Select(item => BuildProduct(item, categoryId))
                .OfType<ProductListItem>()
                .ToList();

            return products is { Count: > 0 }
                ? products
                : Array.Empty<ProductListItem>();
        }
        catch
        {
            return Array.Empty<ProductListItem>();
        }
    }

    private static ProductListItem? BuildProduct(ProductListApiItem item, string fallbackCategoryId)
    {
        var code = item.Code?.Trim();
        var name = item.Name?.Trim();
        var categoryId = string.IsNullOrWhiteSpace(item.Catalog)
            ? fallbackCategoryId.Trim()
            : item.Catalog.Trim();

        if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var id = !string.IsNullOrWhiteSpace(code)
            ? code
            : name!;

        var modelName = !string.IsNullOrWhiteSpace(name)
            ? name
            : code!;

        return new ProductListItem(
            id,
            categoryId,
            modelName,
            ParsePrice(item.Price),
            string.IsNullOrWhiteSpace(item.PictureUrl) ? null : item.PictureUrl.Trim());
    }

    private static decimal ParsePrice(string? price)
    {
        return decimal.TryParse(
            price,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var value)
            ? value
            : 0m;
    }

    private sealed class ProductListResponse
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("msg")]
        public string? Message { get; set; }

        [JsonPropertyName("result")]
        public ProductListResult? Result { get; set; }
    }

    private sealed class ProductListResult
    {
        [JsonPropertyName("goods")]
        public List<ProductListApiItem>? Goods { get; set; }
    }

    private sealed class ProductListApiItem
    {
        [JsonPropertyName("catelog")]
        public string? Catalog { get; set; }

        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("price")]
        public string? Price { get; set; }

        [JsonPropertyName("pic")]
        public string? PictureUrl { get; set; }
    }
}
