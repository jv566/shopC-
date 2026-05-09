namespace Shop.Maui.Views;

public partial class Image2CategoryPage : ContentPage
{
    private const double DesignWidth = 1920;
    private const double DesignHeight = 1080;

    public Image2CategoryPage(ViewModels.ProductListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is ViewModels.ProductListViewModel vm)
        {
            await vm.InitializeAsync();
        }
    }

    private void OnViewportSizeChanged(object? sender, EventArgs e)
    {
        if (Viewport.Width <= 0 || Viewport.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(Viewport.Width / DesignWidth, Viewport.Height / DesignHeight);

        DesignRoot.Scale = scale;
        DesignRoot.TranslationX = (Viewport.Width - DesignWidth * scale) / 2;
        DesignRoot.TranslationY = (Viewport.Height - DesignHeight * scale) / 2;
    }
}
