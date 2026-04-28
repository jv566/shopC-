using Shop.Maui.Models;

namespace Shop.Maui.Services;

public sealed class MockProductCategoryProvider : IProductCategoryProvider
{
    public Task<IReadOnlyList<ProductCategoryOption>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ProductCategoryCatalog.GetPrimaryCategories());
    }
}
