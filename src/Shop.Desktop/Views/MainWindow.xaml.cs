using System.Windows;
using Shop.Desktop.Services;
using Shop.Desktop.Views.Pages;

namespace Shop.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        DesktopServices.Navigation.Navigated += OnNavigated;
        PageHost.Content = new HomePageView();
    }

    private void OnNavigated(System.Windows.Controls.UserControl view)
    {
        PageHost.Content = view;
    }

    protected override void OnClosed(EventArgs e)
    {
        DesktopServices.Navigation.Navigated -= OnNavigated;
        base.OnClosed(e);
    }
}
