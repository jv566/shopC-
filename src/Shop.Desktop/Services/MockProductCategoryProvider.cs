using Shop.Desktop.Models;

namespace Shop.Desktop.Services;

public sealed class MockProductCategoryProvider : IProductCategoryProvider
{
    // Temporary local data. Replace with backend API implementation later.
    public Task<IReadOnlyList<ProductCategoryOption>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ProductCategoryCatalog.GetPrimaryCategories());
    }
}
