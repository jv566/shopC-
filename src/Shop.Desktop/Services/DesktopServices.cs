namespace Shop.Desktop.Services;

public static class DesktopServices
{
    public static IPageNavigationService Navigation { get; } = new PageNavigationService();

    public static IProductCategoryProvider ProductCategoryProvider { get; } = new MockProductCategoryProvider();

    public static IProductCategoryTreeProvider ProductCategoryTreeProvider { get; } = new MockProductCategoryTreeProvider();

    public static IHomeCarouselProvider HomeCarouselProvider { get; } = new MockHomeCarouselProvider();

    public static IProductProvider ProductProvider { get; } = new MockProductProvider();

    public static IProductColorVariantProvider ProductColorVariantProvider { get; } = new MockProductColorVariantProvider();
}
