using System.Collections.ObjectModel;
using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Shop.Maui.Models;
using Shop.Maui.Services;

namespace Shop.Maui.ViewModels;

public sealed class ProductDetailViewModel : ObservableObject, IQueryAttributable
{
    private const string ProductDetailBaseUrl =
        "https://www.ruanzi.net/jy/go/we.aspx?ituid=121&itjid=12106&itcid=12106";
    private const string ProductDescriptionBaseUrl =
        "https://www.ruanzi.net/jy/go/we.aspx?ituid=121&itwid=05&itcid=12105";

    private static readonly HttpClient HttpClient = new();

    private readonly IProductColorVariantProvider _colorVariantProvider;
    private readonly IUserActionService _userActionService;
    private readonly IImageCacheService _imageCacheService;

    private bool _isInitialized;
    private int _currentColorIndex;
    private string _requestedProductId = string.Empty;

    public ProductListItem Product { get; private set; } = new("", "", "", 0m, null);

    private string _productCodeText = string.Empty;
    public string ProductCodeText
    {
        get => _productCodeText;
        private set => SetProperty(ref _productCodeText, value);
    }

    private string _productModelText = string.Empty;
    public string ProductModelText
    {
        get => _productModelText;
        private set => SetProperty(ref _productModelText, value);
    }

    private string _productPriceText = string.Empty;
    public string ProductPriceText
    {
        get => _productPriceText;
        private set => SetProperty(ref _productPriceText, value);
    }

    private string _productOriginalPriceText = string.Empty;
    public string ProductOriginalPriceText
    {
        get => _productOriginalPriceText;
        private set => SetProperty(ref _productOriginalPriceText, value);
    }

    private string _productDescriptionText = "暂无介绍";
    public string ProductDescriptionText
    {
        get => _productDescriptionText;
        private set => SetProperty(ref _productDescriptionText, value);
    }

    private WebViewSource _productDescriptionSource = CreateDescriptionSource(null, "介绍加载中...");
    public WebViewSource ProductDescriptionSource
    {
        get => _productDescriptionSource;
        private set => SetProperty(ref _productDescriptionSource, value);
    }

    private string _productImageSource = "product_bed.png";
    public string ProductImageSource
    {
        get => _productImageSource;
        private set => SetProperty(ref _productImageSource, value);
    }

    public ObservableCollection<ProductColorImageOption> ColorOptions { get; } = [];

    public ObservableCollection<ProductSpecOptionGroup> SpecOptionGroups { get; } = [];

    public int CurrentColorIndex
    {
        get => _currentColorIndex;
        private set
        {
            if (SetProperty(ref _currentColorIndex, value))
            {
                OnPropertyChanged(nameof(ColorIndexText));
                OnPropertyChanged(nameof(SelectedOptionText));
                OnPropertyChanged(nameof(CanSwitchColor));
            }
        }
    }

    public bool CanSwitchColor => ColorOptions.Count > 1;

    public string ColorIndexText => ColorOptions.Count == 0 ? "0/0" : $"{CurrentColorIndex + 1}/{ColorOptions.Count}";

    public string SelectedOptionText
    {
        get
        {
            var option = GetCurrentColorOption();
            return option is null ? "未选择" : $"已选：{option.SpecName} {option.OptionName}";
        }
    }

    public ICommand SelectColorCommand { get; }
    public ICommand AddToCartCommand { get; }
    public ICommand BuyNowCommand { get; }

    public ProductDetailViewModel(
        IProductColorVariantProvider colorVariantProvider,
        IUserActionService userActionService,
        IImageCacheService imageCacheService)
    {
        _colorVariantProvider = colorVariantProvider;
        _userActionService = userActionService;
        _imageCacheService = imageCacheService;
        SelectColorCommand = new Command<int>(index => SetCurrentColorIndex(index));
        AddToCartCommand = new Command(async () => await ShowActionResultAsync(_userActionService.AddToCartAsync(Product)));
        BuyNowCommand = new Command(async () => await ShowActionResultAsync(_userActionService.BuyNowAsync(Product)));
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var productId = query.TryGetValue("productId", out var pid) ? pid as string : null;
        var modelName = query.TryGetValue("modelName", out var name) ? name as string : null;
        var imageUrl = query.TryGetValue("imageUrl", out var img) ? img as string : null;
        var salePrice = query.TryGetValue("salePrice", out var priceObj) &&
                        priceObj is string priceStr &&
                        decimal.TryParse(priceStr, NumberStyles.Number, CultureInfo.InvariantCulture, out var price)
            ? price
            : 0m;

        if (string.IsNullOrWhiteSpace(productId) || string.IsNullOrWhiteSpace(modelName))
        {
            return;
        }

        _requestedProductId = productId.Trim();
        Product = new ProductListItem(productId, string.Empty, modelName, salePrice, imageUrl);
        ApplyProductSnapshot(Product, null, null);
        ProductDescriptionSource = CreateDescriptionSource(null, "介绍加载中...");
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
        {
            return;
        }

        var productCode = GetRequestedProductId();
        var detail = await FetchProductDetailAsync(productCode, cancellationToken);
        if (detail is not null)
        {
            Product = new ProductListItem(
                string.IsNullOrWhiteSpace(detail.Code) ? Product.Id : detail.Code,
                Product.CategoryId,
                string.IsNullOrWhiteSpace(detail.Name) ? Product.ModelName : detail.Name,
                detail.Price ?? Product.SalePrice,
                string.IsNullOrWhiteSpace(detail.ImageUrl) ? Product.ImageUrl : detail.ImageUrl);

            ApplyProductSnapshot(Product, detail.OriginalPriceText, detail.Description);
        }

        var descriptionHtml = detail is null
            ? string.Empty
            : await FetchProductDescriptionHtmlAsync(productCode, cancellationToken);
        ProductDescriptionSource = string.IsNullOrWhiteSpace(descriptionHtml)
            ? CreateDescriptionSource(ProductDescriptionText, "暂无介绍")
            : CreateDescriptionSource(descriptionHtml);

        var variants = detail?.Variants;
        if (variants is null || variants.Count == 0)
        {
            variants = detail is null
                ? Array.Empty<ProductColorVariant>()
                : await _colorVariantProvider.GetColorVariantsAsync(Product, cancellationToken);
        }

        var options = variants
            .Select((x, index) =>
            {
                var (specName, optionName) = SplitVariantName(x.ColorName);
                return new ProductColorImageOption(index, x.ColorName, x.ImageUrl, specName, optionName);
            })
            .ToList();

        if (options.Count == 0)
        {
            options =
            [
                new ProductColorImageOption(0, "默认色", Product.ImageUrl, "颜色", "默认色")
            ];
        }

        ReplaceCollection(ColorOptions, options);
        ReplaceCollection(
            SpecOptionGroups,
            options
                .GroupBy(x => x.SpecName)
                .Select(group => new ProductSpecOptionGroup(group.Key, group)));
        SetCurrentColorIndex(0);
        _isInitialized = true;
    }

    public bool IsValidColorIndex(int targetIndex)
    {
        return targetIndex >= 0 && targetIndex < ColorOptions.Count;
    }

    public ProductColorImageOption? GetColorOption(int targetIndex)
    {
        if (!IsValidColorIndex(targetIndex))
        {
            return null;
        }

        return ColorOptions[targetIndex];
    }

    public ProductColorImageOption? GetCurrentColorOption()
    {
        return GetColorOption(CurrentColorIndex);
    }

    public void SetCurrentColorIndex(int targetIndex)
    {
        if (!IsValidColorIndex(targetIndex))
        {
            return;
        }

        CurrentColorIndex = targetIndex;

        for (var i = 0; i < ColorOptions.Count; i++)
        {
            ColorOptions[i].IsSelected = i == targetIndex;
        }

        var imageSource = ColorOptions[targetIndex].ImageUrl;
        ApplyImageSource(imageSource);
        _ = RefreshSelectedImageAsync(targetIndex, imageSource);
    }

    public bool TryGetRelativeTargetIndex(int step, out int targetIndex, out int direction)
    {
        targetIndex = CurrentColorIndex;
        direction = 1;

        if (!CanSwitchColor)
        {
            return false;
        }

        targetIndex = CurrentColorIndex + step;

        if (targetIndex < 0)
        {
            targetIndex = ColorOptions.Count - 1;
            direction = -1;
        }
        else if (targetIndex >= ColorOptions.Count)
        {
            targetIndex = 0;
            direction = 1;
        }

        return true;
    }

    private void ApplyProductSnapshot(
        ProductListItem product,
        string? originalPriceText,
        string? description)
    {
        ProductCodeText = string.IsNullOrWhiteSpace(product.Id) ? "-" : product.Id;
        ProductModelText = string.IsNullOrWhiteSpace(product.ModelName) ? ProductCodeText : product.ModelName;
        ProductPriceText = FormatPrice(product.SalePrice);
        ProductOriginalPriceText = CleanPlaceholder(originalPriceText);
        ProductDescriptionText = string.IsNullOrWhiteSpace(CleanPlaceholder(description))
            ? "暂无介绍"
            : CleanPlaceholder(description);

        ApplyImageSource(product.ImageUrl);
    }

    private void ApplyImageSource(string? imageSource)
    {
        ProductImageSource = string.IsNullOrWhiteSpace(imageSource)
            ? "product_bed.png"
            : _imageCacheService.GetBestImageSource(imageSource);
    }

    private async Task RefreshSelectedImageAsync(int targetIndex, string? imageSource)
    {
        if (string.IsNullOrWhiteSpace(imageSource))
        {
            return;
        }

        var cachedSource = await _imageCacheService.GetCachedImageSourceAsync(imageSource);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (CurrentColorIndex == targetIndex)
            {
                ProductImageSource = cachedSource;
            }
        });
    }

    private async Task<ProductDetailPayload?> FetchProductDetailAsync(
        string productId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            return null;
        }

        try
        {
            var requestUrl =
                $"{ProductDetailBaseUrl}&keyvalue={Uri.EscapeDataString(productId.Trim())}";

            using var response = await HttpClient.GetAsync(requestUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var json = ExtractJsonObject(content);

            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            using var document = JsonDocument.Parse(RepairProductDetailJson(json));

            var root = document.RootElement;
            JsonElement data;
            if (root.TryGetProperty("result", out var result))
            {
                data = result;
            }
            else if (root.TryGetProperty("data", out var legacyData))
            {
                data = legacyData;
            }
            else
            {
                return null;
            }

            var imageUrl = GetFirstSkuImage(data);
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                imageUrl = GetString(data, "pic");
            }

            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                imageUrl = GetString(data, "\u56fe\u7247");
            }

            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                imageUrl = GetFirstMainPicture(data);
            }

            var payload = new ProductDetailPayload(
                FirstNonEmpty(GetString(data, "code"), GetString(data, "spuCode"), GetString(data, "id")),
                GetString(data, "name"),
                ParsePrice(GetString(data, "price")),
                FirstNonEmpty(GetString(data, "oldPrice"), GetString(data, "yprice")),
                imageUrl,
                FirstNonEmpty(GetString(data, "desc"), GetString(data, "info")),
                BuildSpecVariants(data, imageUrl));

            return IsDetailMatch(productId, payload, data)
                ? payload
                : null;
        }
        catch
        {
            return null;
        }
    }

    private async Task<string> FetchProductDescriptionHtmlAsync(
        string productId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            return string.Empty;
        }

        try
        {
            var requestUrl =
                $"{ProductDescriptionBaseUrl}&keyvalue={Uri.EscapeDataString(productId.Trim())}";

            using var response = await HttpClient.GetAsync(requestUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return StripOuterHtml(content);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string ExtractJsonObject(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        var startIndex = content.IndexOf('{');
        var endIndex = content.LastIndexOf('}');

        return startIndex >= 0 && endIndex > startIndex
            ? content[startIndex..(endIndex + 1)]
            : content.Trim();
    }

    private static string RepairProductDetailJson(string json)
    {
        return Regex.Replace(json, @":\s*,", ": null,");
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) &&
               property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;
    }

    private static string FirstNonEmpty(params string[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(CleanPlaceholder(value))) ?? string.Empty;
    }

    private static string GetFirstSkuImage(JsonElement data)
    {
        if (!data.TryGetProperty("skus", out var skus) ||
            skus.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        foreach (var sku in skus.EnumerateArray())
        {
            var picture = GetString(sku, "picture");
            if (!string.IsNullOrWhiteSpace(picture))
            {
                return picture;
            }
        }

        return string.Empty;
    }

    private static string GetFirstMainPicture(JsonElement data)
    {
        if (!data.TryGetProperty("mainPictures", out var pictures) ||
            pictures.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        foreach (var picture in pictures.EnumerateArray())
        {
            if (picture.ValueKind == JsonValueKind.String)
            {
                var value = picture.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }
        }

        return string.Empty;
    }

    private static bool IsDetailMatch(string requestedProductId, ProductDetailPayload payload, JsonElement data)
    {
        var requested = NormalizeProductCode(requestedProductId);
        if (string.IsNullOrWhiteSpace(requested))
        {
            return false;
        }

        var candidates = new List<string?>
        {
            payload.Code,
            GetString(data, "id"),
            GetString(data, "code"),
            GetString(data, "spuCode")
        };

        if (data.TryGetProperty("skus", out var skus) &&
            skus.ValueKind == JsonValueKind.Array)
        {
            foreach (var sku in skus.EnumerateArray())
            {
                candidates.Add(GetString(sku, "id"));
                candidates.Add(GetString(sku, "skuCode"));
                candidates.Add(GetString(sku, "code"));
            }
        }

        return candidates
            .Select(NormalizeProductCode)
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate))
            .Any(candidate =>
                string.Equals(candidate, requested, StringComparison.OrdinalIgnoreCase) ||
                candidate.Contains(requested, StringComparison.OrdinalIgnoreCase) ||
                requested.Contains(candidate, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeProductCode(string? value)
    {
        return CleanPlaceholder(value)
            .Trim()
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .ToUpperInvariant();
    }

    private static IReadOnlyList<ProductColorVariant> BuildSpecVariants(JsonElement data, string fallbackImageUrl)
    {
        if (!data.TryGetProperty("specs", out var specs) ||
            specs.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<ProductColorVariant>();
        }

        var variants = new List<ProductColorVariant>();

        foreach (var spec in specs.EnumerateArray())
        {
            var specName = CleanPlaceholder(GetString(spec, "name"));
            if (!spec.TryGetProperty("values", out var values) ||
                values.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var value in values.EnumerateArray())
            {
                var valueName = CleanPlaceholder(GetString(value, "name"));
                if (string.IsNullOrWhiteSpace(valueName))
                {
                    continue;
                }

                var label = string.IsNullOrWhiteSpace(specName)
                    ? valueName
                    : $"{specName}：{valueName}";

                var picture = FirstNonEmpty(GetString(value, "picture"), fallbackImageUrl);
                variants.Add(new ProductColorVariant(label, picture));
            }
        }

        return variants;
    }

    private static decimal? ParsePrice(string? price)
    {
        return decimal.TryParse(
            price,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var value)
            ? value
            : null;
    }

    private static string FormatPrice(decimal price)
    {
        return price > 0 ? $"¥{price:F2}" : "¥0.00";
    }

    private static string CleanPlaceholder(string? value)
    {
        var text = (value ?? string.Empty).Trim();
        return text.StartsWith("[#", StringComparison.Ordinal) && text.EndsWith(']')
            ? string.Empty
            : text;
    }

    private string GetProductDescriptionKey()
    {
        return string.IsNullOrWhiteSpace(ProductCodeText) || ProductCodeText == "-"
            ? Product.Id
            : ProductCodeText;
    }

    private string GetRequestedProductId()
    {
        return string.IsNullOrWhiteSpace(_requestedProductId)
            ? GetProductDescriptionKey()
            : _requestedProductId;
    }

    private static WebViewSource CreateDescriptionSource(string? html, string? fallbackText = null)
    {
        var body = CleanPlaceholder(html);
        if (string.IsNullOrWhiteSpace(body))
        {
            body = $"""
                <div class="empty">{WebUtility.HtmlEncode(fallbackText ?? "暂无介绍")}</div>
                """;
        }
        else if (!LooksLikeHtml(body))
        {
            body = $"<p>{WebUtility.HtmlEncode(body).Replace("\n", "<br>", StringComparison.Ordinal)}</p>";
        }

        return new HtmlWebViewSource
        {
            Html = $$"""
                <!doctype html>
                <html>
                <head>
                    <meta charset="utf-8">
                    <meta name="viewport" content="width=device-width, initial-scale=1">
                    <style>
                        html, body {
                            margin: 0;
                            padding: 0;
                            background: #102947;
                            color: #ffffff;
                            font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
                            font-size: 24px;
                            line-height: 1.7;
                            overflow-x: hidden;
                        }
                        body {
                            padding: 18px 18px 26px;
                            box-sizing: border-box;
                        }
                        p {
                            margin: 0 0 18px;
                        }
                        img {
                            display: block;
                            width: auto;
                            max-width: 100%;
                            height: auto;
                            margin: 0 auto 18px;
                            border-radius: 8px;
                        }
                        br {
                            line-height: 1.2;
                        }
                        .empty {
                            min-height: 220px;
                            display: flex;
                            align-items: center;
                            justify-content: center;
                            text-align: center;
                            color: #b8d6f5;
                        }
                    </style>
                </head>
                <body>{{body}}</body>
                </html>
                """
        };
    }

    private static bool LooksLikeHtml(string value)
    {
        return value.Contains('<', StringComparison.Ordinal) &&
               value.Contains('>', StringComparison.Ordinal);
    }

    private static string StripOuterHtml(string content)
    {
        var html = CleanPlaceholder(content);
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        html = Regex.Replace(html, @"<!doctype[^>]*>", string.Empty, RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"</?html[^>]*>", string.Empty, RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"</?body[^>]*>", string.Empty, RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<head[^>]*>.*?</head>", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);

        return html.Trim();
    }

    private static (string SpecName, string OptionName) SplitVariantName(string variantName)
    {
        var text = CleanPlaceholder(variantName);
        if (string.IsNullOrWhiteSpace(text))
        {
            return ("可选类型", "默认");
        }

        var separatorIndex = text.IndexOfAny(['：', ':']);
        if (separatorIndex <= 0 || separatorIndex >= text.Length - 1)
        {
            return ("颜色", text);
        }

        return (text[..separatorIndex].Trim(), text[(separatorIndex + 1)..].Trim());
    }

    private static async Task ShowActionResultAsync(Task<UserActionResult> actionTask)
    {
        var result = await actionTask;
        if (Shell.Current is not null)
        {
            await Shell.Current.DisplayAlert(result.Title, result.Message, "确定");
        }
    }

    private sealed record ProductDetailPayload(
        string Code,
        string Name,
        decimal? Price,
        string OriginalPriceText,
        string ImageUrl,
        string Description,
        IReadOnlyList<ProductColorVariant> Variants);
}
