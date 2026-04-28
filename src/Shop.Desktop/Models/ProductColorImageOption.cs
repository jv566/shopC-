using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace Shop.Desktop.Models;

public sealed class ProductColorImageOption : INotifyPropertyChanged
{
    private static readonly Brush DefaultColorButtonBackground = CreateFrozenBrush(0xF2, 0xF2, 0xF2);
    private static readonly Brush SelectedColorButtonBackground = CreateFrozenBrush(0xFF, 0xF5, 0x5D);

    private bool _isSelected;

    public ProductColorImageOption(int index, string colorName, string? imageUrl)
    {
        Index = index;
        ColorName = colorName;
        ImageUrl = imageUrl;
    }

    public int Index { get; }

    public string ColorName { get; }

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
            OnPropertyChanged(nameof(ButtonBackground));
            OnPropertyChanged(nameof(ButtonBorderThickness));
        }
    }

    public Brush ButtonBackground => IsSelected ? SelectedColorButtonBackground : DefaultColorButtonBackground;

    public Thickness ButtonBorderThickness => IsSelected ? new Thickness(2) : new Thickness(1);

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private static Brush CreateFrozenBrush(byte red, byte green, byte blue)
    {
        var brush = new SolidColorBrush(Color.FromRgb(red, green, blue));
        brush.Freeze();
        return brush;
    }
}
