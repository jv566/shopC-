using Shop.Maui.Models;

namespace Shop.Maui.Services;

public sealed class MockProductCategoryTreeProvider : IProductCategoryTreeProvider
{
    public Task<IReadOnlyList<ProductCategoryGroup>> GetCategoryTreeAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ProductCategoryCatalog.GetCategoryTree());
    }
}
