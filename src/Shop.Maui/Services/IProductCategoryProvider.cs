using Shop.Maui.Models;

namespace Shop.Maui.Services;

public interface IProductCategoryProvider
{
    Task<IReadOnlyList<ProductCategoryOption>> GetCategoriesAsync(CancellationToken cancellationToken = default);
}
