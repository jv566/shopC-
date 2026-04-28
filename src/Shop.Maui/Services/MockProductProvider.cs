using Shop.Maui.Models;

namespace Shop.Maui.Services;

public sealed class MockProductProvider : IProductProvider
{
    public Task<IReadOnlyList<ProductListItem>> GetProductsAsync(string categoryId, CancellationToken cancellationToken = default)
    {
        var allProducts = new List<ProductListItem>
        {
            new("p-bed-wood-001", "bed-wood", "木板床 M100", 2899m, null),
            new("p-bed-leather-001", "bed-leather", "真皮床 Z100", 4599m, null),
            new("p-bed-fabric-001", "bed-fabric", "布艺床 B100", 3699m, null),

            new("p-sofa-leather-001", "sofa-leather", "真皮沙发 S100", 5299m, null),
            new("p-sofa-fabric-001", "sofa-fabric", "布艺沙发 S200", 3899m, null),
            new("p-sofa-corner-001", "sofa-corner", "转角沙发 S300", 6299m, null),

            new("p-table-dining-001", "table-dining", "餐桌 T100", 2599m, null),
            new("p-table-coffee-001", "table-coffee", "茶几 C100", 1399m, null),
            new("p-table-side-001", "table-side", "边几 S100", 899m, null),

            new("p-wardrobe-sliding-001", "wardrobe-sliding", "推拉门衣柜 W100", 3999m, null),
            new("p-wardrobe-hinged-001", "wardrobe-hinged", "平开门衣柜 W200", 4299m, null),
            new("p-wardrobe-custom-001", "wardrobe-custom", "定制衣柜 W300", 5699m, null),

            new("p-custom-tv-001", "custom-tv", "电视柜 C100", 2199m, null),
            new("p-custom-entry-001", "custom-entry", "玄关柜 C200", 1899m, null),
            new("p-custom-cabinet-001", "custom-cabinet", "多功能柜 C300", 2999m, null)
        };

        var group = ProductCategoryCatalog.ResolvePrimaryGroup(categoryId);
        List<ProductListItem> products;

        if (group is null)
        {
            products = allProducts.Take(6).ToList();
            return Task.FromResult<IReadOnlyList<ProductListItem>>(products);
        }

        var key = (categoryId ?? string.Empty).Trim();
        var hasSecondaryHit = !string.IsNullOrWhiteSpace(key) &&
            group.SecondaryCategories.Any(s =>
                string.Equals(s.Id, key, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s.DisplayName, key, StringComparison.OrdinalIgnoreCase));

        if (hasSecondaryHit)
        {
            var selectedSecondary = ProductCategoryCatalog.ResolveSecondaryCategory(key, group.PrimaryCategory.Id);
            products = selectedSecondary is null
                ? new List<ProductListItem>()
                : allProducts.Where(p => string.Equals(p.CategoryId, selectedSecondary.Id, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        else
        {
            var secondaryIds = ProductCategoryCatalog
                .GetSecondaryCategories(group.PrimaryCategory.Id)
                .Select(x => x.Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            products = allProducts.Where(p => secondaryIds.Contains(p.CategoryId)).ToList();
        }

        if (products.Count == 0)
        {
            products = allProducts.Take(6).ToList();
        }

        return Task.FromResult<IReadOnlyList<ProductListItem>>(products);
    }
}
