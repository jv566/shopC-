using Shop.Maui.Models;

namespace Shop.Maui.Services;

public interface IProductProvider
{
    Task<IReadOnlyList<ProductListItem>> GetProductsAsync(string categoryId, CancellationToken cancellationToken = default);
}
