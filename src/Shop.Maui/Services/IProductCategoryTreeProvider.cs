using Shop.Maui.Models;

namespace Shop.Maui.Services;

public interface IProductCategoryTreeProvider
{
    Task<IReadOnlyList<ProductCategoryGroup>> GetCategoryTreeAsync(CancellationToken cancellationToken = default);
}
