using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using System.Windows.Input;
using Shop.Maui.Models;
using Shop.Maui.Services;

namespace Shop.Maui.ViewModels;

public sealed class ProductDetailViewModel : ObservableObject, IQueryAttributable
{
    private const string ProductDetailBaseUrl =
        "https://www.ruanzi.net/jy/go/we.aspx?ituid=121&itwid=12104&itcid=12104";

    private static readonly HttpClient HttpClient = new();

    private readonly IProductColorVariantProvider _colorVariantProvider;
    private readonly IUserActionService _userActionService;
    private readonly IImageCacheService _imageCacheService;

    private bool _isInitialized;
    private int _currentColorIndex;

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

    private string _productImageSource = "product_bed.png";
    public string ProductImageSource
    {
        get => _productImageSource;
        private set => SetProperty(ref _productImageSource, value);
    }

    public ObservableCollection<ProductColorImageOption> ColorOptions { get; } = [];

    public int CurrentColorIndex
    {
        get => _currentColorIndex;
        private set
        {
            if (SetProperty(ref _currentColorIndex, value))
            {
                OnPropertyChanged(nameof(ColorIndexText));
                OnPropertyChanged(nameof(CanSwitchColor));
            }
        }
    }

    public bool CanSwitchColor => ColorOptions.Count > 1;

    public string ColorIndexText => ColorOptions.Count == 0 ? "0/0" : $"{CurrentColorIndex + 1}/{ColorOptions.Count}";

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

        Product = new ProductListItem(productId, string.Empty, modelName, salePrice, imageUrl);
        ApplyProductSnapshot(Product, null, null);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
        {
            return;
        }

        var detail = await FetchProductDetailAsync(Product.Id, cancellationToken);
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

        var variants = await _colorVariantProvider.GetColorVariantsAsync(Product, cancellationToken);

        var options = variants
            .Select((x, index) => new ProductColorImageOption(index, x.ColorName, x.ImageUrl))
            .ToList();

        if (options.Count == 0)
        {
            options =
            [
                new ProductColorImageOption(0, "默认色", Product.ImageUrl)
            ];
        }

        ReplaceCollection(ColorOptions, options);
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

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (!document.RootElement.TryGetProperty("data", out var data))
            {
                return null;
            }

            return new ProductDetailPayload(
                GetString(data, "code"),
                GetString(data, "name"),
                ParsePrice(GetString(data, "price")),
                GetString(data, "yprice"),
                GetString(data, "\u56fe\u7247"),
                GetString(data, "info"));
        }
        catch
        {
            return null;
        }
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) &&
               property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;
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
        string Description);
}
