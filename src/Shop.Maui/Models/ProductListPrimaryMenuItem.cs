using System.ComponentModel;          // 引入 INotifyPropertyChanged、PropertyChangedEventHandler 等类型
using System.Runtime.CompilerServices; // 引入 CallerMemberName，用来自动获取调用者属性名

namespace Shop.Maui.Models;

// 左侧一级分类菜单项
// 作用：保存一级分类的信息，并根据是否选中切换颜色、背景图、图标
public sealed class ProductListPrimaryMenuItem : INotifyPropertyChanged
{
    // 私有字段：记录当前菜单项是否被选中
    private bool _isSelected;

    // 构造方法：创建这个菜单项时，必须传入一个分类组
    public ProductListPrimaryMenuItem(ProductCategoryGroup group)
    {
        Group = group;
    }

    // 当前菜单项对应的分类组
    // get; 表示外部只能读取，不能重新赋值
    public ProductCategoryGroup Group { get; }

    // 一级分类 ID
    // => 是简写，等价于 get { return Group.PrimaryCategory.Id; }
    public string Id => Group.PrimaryCategory.Id;

    // 一级分类显示名称，比如“床”“沙发”“桌子”
    public string DisplayName => Group.PrimaryCategory.DisplayName;

    // 二级分类列表
    // IReadOnlyList 表示只读列表，外部不能随便修改
    public IReadOnlyList<ProductCategoryOption> SecondaryCategories => Group.SecondaryCategories;

    // 是否被选中
    public bool IsSelected
    {
        get => _isSelected; // 读取当前选中状态

        set
        {
            // 如果新值和旧值一样，就不用继续处理
            if (_isSelected == value)
            {
                return;
            }

            // 更新选中状态
            _isSelected = value;

            // 通知界面：IsSelected 变了
            // 因为这里没传参数，CallerMemberName 会自动传入 "IsSelected"
            OnPropertyChanged();

            // 下面这些属性都依赖 IsSelected
            // 所以 IsSelected 改变后，也要通知界面重新读取它们

            // 背景颜色变化
            OnPropertyChanged(nameof(BackgroundColor));

            // 边框颜色变化
            OnPropertyChanged(nameof(StrokeColor));

            // 文字颜色变化
            OnPropertyChanged(nameof(TextColor));

            // 背景图片变化
            OnPropertyChanged(nameof(BackgroundImageSource));

            // 图标变化
            OnPropertyChanged(nameof(IconSource));
        }
    }

    // 背景颜色
    // IsSelected 为 true 用浅蓝色，否则用深蓝色
    public Color BackgroundColor => IsSelected
        ? Color.FromArgb("#3F8ED1")
        : Color.FromArgb("#244F82");

    // 边框颜色
    // 选中和未选中使用不同颜色
    public Color StrokeColor => IsSelected
        ? Color.FromArgb("#18F7FF")
        : Color.FromArgb("#3B73AA");

    // 文字颜色，固定为白色
    public Color TextColor => Colors.White;

    // 背景图片
    // 选中时用 menu_item_selected.png
    // 未选中时用 menu_panel.png
    public string BackgroundImageSource => IsSelected
        ? "menu_item_selected.png"
        : "menu_panel.png";

    // 图标路径
    // 根据 分类 ID + 是否选中 来决定使用哪张图片
    public string IconSource => (Id, IsSelected) switch
    {
        // Id 是 bed、01、09，并且被选中，使用床的激活图标
        ("bed" or "01" or "09", true) => "menu_icon_bed_active.png",

        // Id 是 bed、01、09，并且未选中，使用床的白色图标
        ("bed" or "01" or "09", false) => "menu_icon_bed_white.png",

        // Id 是 sofa、02、05，并且被选中
        ("sofa" or "02" or "05", true) => "menu_icon_sofa_active.png",

        // Id 是 sofa、02、05，并且未选中
        ("sofa" or "02" or "05", false) => "menu_icon_sofa_white.png",

        // Id 是 table、03、06、07、10，并且被选中
        ("table" or "03" or "06" or "07" or "10", true) => "menu_icon_table_active.png",

        // Id 是 table、03、06、07、10，并且未选中
        ("table" or "03" or "06" or "07" or "10", false) => "menu_icon_table_white.png",

        // Id 是 wardrobe、04、08，并且被选中
        ("wardrobe" or "04" or "08", true) => "menu_icon_wardrobe_active.png",

        // Id 是 wardrobe、04、08，并且未选中
        ("wardrobe" or "04" or "08", false) => "menu_icon_wardrobe_white.png",

        // Id 是 custom，并且被选中
        ("custom", true) => "menu_icon_custom_active.png",

        // Id 是 custom，并且未选中
        ("custom", false) => "menu_icon_custom_white.png",

        // 其他没匹配到的情况，默认使用床的白色图标
        _ => "menu_icon_bed_white.png"
    };

    // 属性变化事件
    // XAML 绑定会监听这个事件，用来刷新界面
    public event PropertyChangedEventHandler? PropertyChanged;

    // 通知界面某个属性发生变化
    // [CallerMemberName] 的作用：
    // 如果调用 OnPropertyChanged() 时不传属性名，
    // 它会自动使用调用它的属性名，比如 IsSelected
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        // ?. 表示如果 PropertyChanged 不为空，才执行 Invoke
        // this 表示当前对象
        // propertyName 表示哪个属性变了
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
