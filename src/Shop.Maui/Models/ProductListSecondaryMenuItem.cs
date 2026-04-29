using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Shop.Maui.Models;

public sealed class ProductListSecondaryMenuItem : INotifyPropertyChanged
{
    private bool _isSelected;

    public ProductListSecondaryMenuItem(ProductCategoryOption category)
    {
        Category = category;
    }

    public ProductCategoryOption Category { get; }

    public string Id => Category.Id;

    public string DisplayName => Category.DisplayName;

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
            OnPropertyChanged(nameof(TextColor));
        }
    }

    public Color TextColor => IsSelected ? Colors.White : Color.FromArgb("#E4F4FF");

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
