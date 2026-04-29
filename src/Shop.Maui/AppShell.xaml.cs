namespace Shop.Maui;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute("productlist", typeof(Views.ProductListPage));
        Routing.RegisterRoute("productdetail", typeof(Views.ProductDetailPage));
    }
}
