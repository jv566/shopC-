using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Shop.Maui.Models;

public sealed class ProductListPrimaryMenuItem : INotifyPropertyChanged
{
    private bool _isSelected;

    public ProductListPrimaryMenuItem(ProductCategoryGroup group)
    {
        Group = group;
    }

    public ProductCategoryGroup Group { get; }

    public string Id => Group.PrimaryCategory.Id;

    public string DisplayName => Group.PrimaryCategory.DisplayName;

    public IReadOnlyList<ProductCategoryOption> SecondaryCategories => Group.SecondaryCategories;

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
            OnPropertyChanged(nameof(BackgroundColor));
            OnPropertyChanged(nameof(StrokeColor));
            OnPropertyChanged(nameof(TextColor));
            OnPropertyChanged(nameof(BackgroundImageSource));
            OnPropertyChanged(nameof(IconSource));
        }
    }

    public Color BackgroundColor => IsSelected ? Color.FromArgb("#4F9CE8") : Color.FromArgb("#3D78BC");

    public Color StrokeColor => IsSelected ? Color.FromArgb("#8DEFFF") : Color.FromArgb("#4D87C9");

    public Color TextColor => Colors.White;

    public string BackgroundImageSource => IsSelected ? "image2/menu_item_selected.png" : "image2/menu_panel.png";

    public string IconSource => (Id, IsSelected) switch
    {
        ("bed", true) => "image2/menu_icon_bed_active.png",
        ("bed", false) => "image2/menu_icon_bed_white.png",
        ("sofa", true) => "image2/menu_icon_sofa_active.png",
        ("sofa", false) => "image2/menu_icon_sofa_white.png",
        ("table", true) => "image2/menu_icon_table_active.png",
        ("table", false) => "image2/menu_icon_table_white.png",
        ("wardrobe", true) => "image2/menu_icon_wardrobe_active.png",
        ("wardrobe", false) => "image2/menu_icon_wardrobe_white.png",
        ("custom", true) => "image2/menu_icon_custom_active.png",
        ("custom", false) => "image2/menu_icon_custom_white.png",
        _ => "image2/menu_icon_bed_white.png"
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
