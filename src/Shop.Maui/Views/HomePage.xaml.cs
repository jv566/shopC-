namespace Shop.Maui.Views;

public partial class HomePage : ContentPage
{
    private const double CategoryDesignWidth = 330;
    private const double CategoryDesignHeight = 520;

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
    }
    private void OnCategoryViewportSizeChanged(object? sender, EventArgs e)
    {
        if (CategoryViewport.Width <= 0 || CategoryViewport.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(
            CategoryViewport.Width / CategoryDesignWidth,
            CategoryViewport.Height / CategoryDesignHeight);

        CategoryRoot.Scale = scale;

        // 关键：缩放后，让左侧商品区域在背景框里居中
        CategoryRoot.TranslationX = (CategoryViewport.Width - CategoryDesignWidth * scale) / 2;
        CategoryRoot.TranslationY = (CategoryViewport.Height - CategoryDesignHeight * scale) / 2;
    }
}