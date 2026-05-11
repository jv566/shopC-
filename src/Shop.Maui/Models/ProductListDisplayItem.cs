using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Shop.Maui.Models;

public sealed class ProductListDisplayItem : INotifyPropertyChanged
{
    private string _imageSource;

    public ProductListDisplayItem(
        ProductListItem product,
        string imageSource,
        string displayPriceText,
        string displayModelText,
        string modelLabelImageSource,
        string priceLabelImageSource,
        string cardBackgroundImageSource,
        bool isPlaceholder = false,
        bool isEmptyState = false)
    {
        Product = product;
        _imageSource = imageSource;
        DisplayPriceText = displayPriceText;
        DisplayModelText = displayModelText;
        ModelLabelImageSource = modelLabelImageSource;
        PriceLabelImageSource = priceLabelImageSource;
        CardBackgroundImageSource = cardBackgroundImageSource;
        IsPlaceholder = isPlaceholder;
        IsEmptyState = isEmptyState;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ProductListItem Product { get; }

    public string ImageSource
    {
        get => _imageSource;
        set
        {
            if (_imageSource == value)
            {
                return;
            }

            _imageSource = value;
            OnPropertyChanged();
        }
    }

    public string DisplayPriceText { get; }

    public string DisplayModelText { get; }

    public string ModelLabelImageSource { get; }

    public string PriceLabelImageSource { get; }

    public string CardBackgroundImageSource { get; }

    public bool IsPlaceholder { get; }

    public bool IsEmptyState { get; }

    public bool CanNavigate => !IsPlaceholder && !IsEmptyState;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
