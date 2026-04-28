using System.Windows.Controls;
using Shop.Desktop.Models;
using Shop.Desktop.Views.Pages;

namespace Shop.Desktop.Services;

public sealed class PageNavigationService : IPageNavigationService
{
    public event Action<UserControl>? Navigated;

    public void NavigateToHome()
    {
        Navigated?.Invoke(new HomePageView());
    }

    public void NavigateToProduct3DShowcase()
    {
        Navigated?.Invoke(new Product3DShowcaseView());
    }

    public void NavigateToProductPanoramaReplacement()
    {
        Navigated?.Invoke(new ProductPanoramaReplacementView());
    }

    public void NavigateToProductList(ProductCategoryOption category)
    {
        Navigated?.Invoke(new ProductListView(category));
    }

    public void NavigateToProductDetail(ProductListItem product)
    {
        Navigated?.Invoke(new ProductDetailView(product));
    }
}
