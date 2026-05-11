using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Shop.Maui.Models;

public sealed class ProductColorImageOption : INotifyPropertyChanged
{
    private bool _isSelected;

    public ProductColorImageOption(
        int index,
        string colorName,
        string? imageUrl,
        string? specName = null,
        string? optionName = null)
    {
        Index = index;
        ColorName = colorName;
        ImageUrl = imageUrl;
        SpecName = string.IsNullOrWhiteSpace(specName) ? "可选类型" : specName.Trim();
        OptionName = string.IsNullOrWhiteSpace(optionName) ? colorName.Trim() : optionName.Trim();
    }

    public int Index { get; }

    public string ColorName { get; }

    public string SpecName { get; }

    public string OptionName { get; }

    public string? ImageUrl { get; }

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
            OnPropertyChanged(nameof(ButtonBackgroundColor));
            OnPropertyChanged(nameof(ButtonBorderWidth));
        }
    }

    public Color ButtonBackgroundColor => IsSelected ? Color.FromRgb(0xFF, 0xF5, 0x5D) : Color.FromRgb(0xF2, 0xF2, 0xF2);

    public double ButtonBorderWidth => IsSelected ? 2 : 1;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
