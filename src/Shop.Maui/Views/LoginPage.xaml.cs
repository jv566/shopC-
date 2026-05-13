namespace Shop.Maui.Views;

public partial class LoginPage : ContentPage
{
    private const double DesignWidth = 1920;
    private const double DesignHeight = 1080;

    public LoginPage(ViewModels.LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
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
