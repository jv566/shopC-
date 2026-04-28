namespace Shop.Maui.Services;

public interface INavigationService
{
    Task GoToHomeAsync();

    Task GoToProductListAsync(string categoryId, string categoryName);

    Task GoToProductDetailAsync(string productId, string modelName, decimal salePrice, string? imageUrl);

    Task GoToProductPanoramaAsync();

    Task GoToProduct3DShowcaseAsync();
}
