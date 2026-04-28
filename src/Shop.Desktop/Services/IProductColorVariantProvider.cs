using Shop.Desktop.Models;

namespace Shop.Desktop.Services;

public interface IProductColorVariantProvider
{
    Task<IReadOnlyList<ProductColorVariant>> GetColorVariantsAsync(ProductListItem product, CancellationToken cancellationToken = default);
}
