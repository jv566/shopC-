using System.Windows.Controls;
using Shop.Desktop.Models;

namespace Shop.Desktop.Services;

public interface IPageNavigationService
{
    event Action<UserControl> Navigated;

    void NavigateToHome();

    void NavigateToProduct3DShowcase();

    void NavigateToProductPanoramaReplacement();

    void NavigateToProductList(ProductCategoryOption category);

    void NavigateToProductDetail(ProductListItem product);
}
