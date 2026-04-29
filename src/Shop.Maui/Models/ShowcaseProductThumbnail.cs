using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Shop.Maui.Models;

public sealed class ShowcaseProductThumbnail : INotifyPropertyChanged
{
    private bool _isSelected;

    public ShowcaseProductThumbnail(ProductListItem product, string imageSource)
    {
        Product = product;
        ImageSource = imageSource;
    }

    public ProductListItem Product { get; }

    public string ImageSource { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(BorderColor));
            OnPropertyChanged(nameof(BackgroundColor));
        }
    }

    public Color BorderColor => IsSelected ? Color.FromArgb("#6DE7FF") : Color.FromArgb("#4F6F98");

    public Color BackgroundColor => IsSelected ? Color.FromArgb("#F3FAFF") : Color.FromArgb("#F7F9FC");

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
