using Shop.Maui.ViewModels;

namespace Shop.Maui.Views;

public partial class Showcase3DPage : ContentPage
{
    public Showcase3DPage(Showcase3DViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is Showcase3DViewModel vm)
        {
            await vm.InitializeAsync();
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//home");
    }
}
