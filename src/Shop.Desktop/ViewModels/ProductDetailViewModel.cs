using System.Collections.ObjectModel;
using Shop.Desktop.Models;
using Shop.Desktop.Services;

namespace Shop.Desktop.ViewModels;

public sealed class ProductDetailViewModel : ObservableObject
{
    private readonly IProductColorVariantProvider _colorVariantProvider;

    public ProductDetailViewModel(ProductListItem product, IProductColorVariantProvider colorVariantProvider)
    {
        Product = product;
        _colorVariantProvider = colorVariantProvider;

        ProductModelText = product.ModelName;
        ProductPriceText = $"￥{product.SalePrice:F2}";
    }

    public ProductListItem Product { get; }

    public string ProductModelText { get; }

    public string ProductPriceText { get; }

    public ObservableCollection<ProductColorImageOption> ColorOptions { get; } = [];

    public int CurrentColorIndex { get; private set; }

    public bool CanSwitchColor => ColorOptions.Count > 1;

    public string ColorIndexText => ColorOptions.Count == 0 ? "0/0" : $"{CurrentColorIndex + 1}/{ColorOptions.Count}";

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

        OnPropertyChanged(nameof(CurrentColorIndex));
        OnPropertyChanged(nameof(CanSwitchColor));
        OnPropertyChanged(nameof(ColorIndexText));
    }

    public bool TryGetRelativeTargetIndex(int step, out int targetIndex, out int direction)
    {
        targetIndex = CurrentColorIndex;
        direction = 1;

        if (!CanSwitchColor)
        {
            return false;
        }

        direction = step >= 0 ? 1 : -1;
        targetIndex = (CurrentColorIndex + step + ColorOptions.Count) % ColorOptions.Count;

        return true;
    }

    public int ResolveDirectionForJump(int targetIndex)
    {
        var count = ColorOptions.Count;
        if (count <= 1)
        {
            return 1;
        }

        var forwardSteps = (targetIndex - CurrentColorIndex + count) % count;
        var backwardSteps = (CurrentColorIndex - targetIndex + count) % count;

        return forwardSteps <= backwardSteps ? 1 : -1;
    }

    public static string BuildImageHintText(string? imageUrl)
    {
        return string.IsNullOrWhiteSpace(imageUrl)
            ? "图片接口: ColorImageUrl（待后端返回）"
            : $"图片接口: {imageUrl}";
    }

    private static void ReplaceCollection<T>(ICollection<T> target, IEnumerable<T> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }
    }
}
