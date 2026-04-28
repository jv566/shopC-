using Shop.Desktop.Models;

namespace Shop.Desktop.Services;

public interface IProductCategoryTreeProvider
{
    Task<IReadOnlyList<ProductCategoryGroup>> GetCategoryTreeAsync(CancellationToken cancellationToken = default);
}
