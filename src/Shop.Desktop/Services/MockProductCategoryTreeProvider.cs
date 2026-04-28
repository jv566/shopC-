using Shop.Desktop.Models;

namespace Shop.Desktop.Services;

public sealed class MockProductCategoryTreeProvider : IProductCategoryTreeProvider
{
    // TODO: 后续改为调用后端分类树接口（例如 GET /api/categories/tree）。
    public Task<IReadOnlyList<ProductCategoryGroup>> GetCategoryTreeAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ProductCategoryCatalog.GetCategoryTree());
    }
}
