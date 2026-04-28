using System.Collections.ObjectModel;
using System.Windows.Input;
using Shop.Maui.Models;
using Shop.Maui.Services;

namespace Shop.Maui.ViewModels;

public sealed class ProductDetailViewModel : ObservableObject, IQueryAttributable
{
    private readonly IProductColorVariantProvider _colorVariantProvider;

    private int _currentColorIndex;

    public ProductListItem Product { get; private set; } = new("", "", "", 0m, null);

    public string ProductModelText { get; private set; } = string.Empty;

    public string ProductPriceText { get; private set; } = string.Empty;

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

    public ProductDetailViewModel(IProductColorVariantProvider colorVariantProvider)
    {
        _colorVariantProvider = colorVariantProvider;
        SelectColorCommand = new Command<int>(index => SetCurrentColorIndex(index));
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var productId = query.TryGetValue("productId", out var pid) ? pid as string : null;
        var modelName = query.TryGetValue("modelName", out var name) ? name as string : null;
        var imageUrl = query.TryGetValue("imageUrl", out var img) ? img as string : null;
        var salePrice = query.TryGetValue("salePrice", out var priceObj) && priceObj is string priceStr && decimal.TryParse(priceStr, out var price)
            ? price : 0m;

        if (!string.IsNullOrWhiteSpace(productId) && !string.IsNullOrWhiteSpace(modelName))
        {
            Product = new ProductListItem(productId, string.Empty, modelName, salePrice, imageUrl);
            ProductModelText = modelName;
            ProductPriceText = $"￥{salePrice:F2}";
        }
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
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
}
