namespace Shop.Maui;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute("image2category", typeof(Views.Image2CategoryPage));
        Routing.RegisterRoute("productdetail", typeof(Views.ProductDetailPage));
        Routing.RegisterRoute("panorama", typeof(Views.PanoramaPage));
        Routing.RegisterRoute("showcase3d", typeof(Views.Showcase3DPage));
    }
}
