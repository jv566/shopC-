namespace Shop.Maui.Views;

public partial class HomePage : ContentPage
{
    /*
        单个商品内容的设计尺寸。
        注意：
        不是 8 个框整体的尺寸。
        是每一个商品格子里面那一套内容的尺寸。
    */
    private const double ProductContentDesignWidth = 180;
    private const double ProductContentDesignHeight = 130;

    /*
        商品内容缩放系数。
        先用 0.92。
        以后你觉得所有商品都偏大，就改小。
        觉得所有商品都偏小，就改大。
    */
    private const double ProductContentScaleRatio = 0.92;

    public HomePage(ViewModels.HomePageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is ViewModels.HomePageViewModel vm)
        {
            await vm.InitializeAsync();
        }

        ApplyCategoryItemScale();
    }

    private void OnCategoryViewportSizeChanged(object? sender, EventArgs e)
    {
        ApplyCategoryItemScale();
    }

    private void ApplyCategoryItemScale()
    {
        if (CategoryViewport.Width <= 0 || CategoryViewport.Height <= 0)
        {
            return;
        }

        /*
            这里的 18 要和 XAML 里的行间距、列间距一致：

            RowDefinitions="1*,18,1*,18,1*,18,1*"
            ColumnDefinitions="1*,18,1*"
        */
        const double columnGap = 18;
        const double rowGap = 18;

        var singleSlotWidth = (CategoryViewport.Width - columnGap) / 2;
        var singleSlotHeight = (CategoryViewport.Height - rowGap * 3) / 4;

        /*
            关键逻辑：
            每一个商品框自己算缩放。
            不是整个 8 宫格一起缩放。
        */
        var scale = Math.Min(
            singleSlotWidth / ProductContentDesignWidth,
            singleSlotHeight / ProductContentDesignHeight);

        scale *= ProductContentScaleRatio;

        SetProductContentScale(scale);
    }

    private void SetProductContentScale(double scale)
    {
        BedContent.Scale = scale;
        SofaContent.Scale = scale;
        DiningTableContent.Scale = scale;
        SideboardContent.Scale = scale;
        DeskContent.Scale = scale;
        MattressContent.Scale = scale;
        CoffeeTableContent.Scale = scale;
        BookcaseContent.Scale = scale;
    }
}