namespace Shop.Desktop.Models;

public sealed record ProductCategoryGroup(
    ProductCategoryOption PrimaryCategory,
    IReadOnlyList<ProductCategoryOption> SecondaryCategories);
