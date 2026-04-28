using Shop.Maui.Models;

namespace Shop.Maui.Services;

public sealed class MockProductColorVariantProvider : IProductColorVariantProvider
{
    public Task<IReadOnlyList<ProductColorVariant>> GetColorVariantsAsync(ProductListItem product, CancellationToken cancellationToken = default)
    {
        var variants = new List<ProductColorVariant>
        {
            new("云雾白", product.ImageUrl),
            new("岩石灰", null),
            new("胡桃棕", null),
            new("曜石黑", null)
        };

        return Task.FromResult<IReadOnlyList<ProductColorVariant>>(variants);
    }
}
