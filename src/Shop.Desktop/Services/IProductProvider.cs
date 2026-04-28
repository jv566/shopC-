using Shop.Desktop.Models;

namespace Shop.Desktop.Services;

public interface IProductProvider
{
    Task<IReadOnlyList<ProductListItem>> GetProductsAsync(string categoryId, CancellationToken cancellationToken = default);
}
