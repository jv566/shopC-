namespace Shop.Maui.Views;

public partial class ProductListPage : ContentPage
{
    public ProductListPage(ViewModels.ProductListViewModel viewModel)
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
}
