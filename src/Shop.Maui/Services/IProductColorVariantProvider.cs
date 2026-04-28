using Shop.Maui.Models;

namespace Shop.Maui.Services;

public interface IProductColorVariantProvider
{
    Task<IReadOnlyList<ProductColorVariant>> GetColorVariantsAsync(ProductListItem product, CancellationToken cancellationToken = default);
}
