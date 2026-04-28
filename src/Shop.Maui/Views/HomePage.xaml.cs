namespace Shop.Maui.Views;

public partial class HomePage : ContentPage
{
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
}
