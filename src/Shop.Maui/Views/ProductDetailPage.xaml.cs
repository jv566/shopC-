namespace Shop.Maui.Views;

public partial class ProductDetailPage : ContentPage
{
    public ProductDetailPage(ViewModels.ProductDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is ViewModels.ProductDetailViewModel vm)
        {
            await vm.InitializeAsync();
        }
    }
}
