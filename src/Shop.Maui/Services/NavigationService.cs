namespace Shop.Maui.Services;

public sealed class NavigationService : INavigationService
{
    public async Task GoToHomeAsync()
    {
        await Shell.Current.GoToAsync("//home");
    }

    public async Task GoToCategoryWallAsync(string categoryId, string categoryName)
    {
        await Shell.Current.GoToAsync($"image2category?categoryId={Uri.EscapeDataString(categoryId)}&categoryName={Uri.EscapeDataString(categoryName)}");
    }

    public async Task GoToProductDetailAsync(string productId, string categoryId, string productType, string modelName, decimal salePrice, string? imageUrl)
    {
        await Shell.Current.GoToAsync($"productdetail?productId={Uri.EscapeDataString(productId)}&categoryId={Uri.EscapeDataString(categoryId)}&productType={Uri.EscapeDataString(productType)}&modelName={Uri.EscapeDataString(modelName)}&salePrice={salePrice}&imageUrl={Uri.EscapeDataString(imageUrl ?? string.Empty)}");
    }

    public async Task GoToProductPanoramaAsync()
    {
        await Shell.Current.GoToAsync("panorama");
    }

    public async Task GoToProduct3DShowcaseAsync()
    {
        await Shell.Current.GoToAsync("showcase3d");
    }

    public async Task GoToCartAsync()
    {
        await Shell.Current.GoToAsync("cart");
    }

    public async Task GoToMyOrdersAsync()
    {
        await Shell.Current.GoToAsync("myorders");
    }

    public async Task GoToHistoryOrdersAsync()
    {
        await Shell.Current.GoToAsync("historyorders");
    }
}
