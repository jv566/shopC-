using Shop.Desktop.Models;

namespace Shop.Desktop.Services;

public interface IProductCategoryProvider
{
    Task<IReadOnlyList<ProductCategoryOption>> GetCategoriesAsync(CancellationToken cancellationToken = default);
}
