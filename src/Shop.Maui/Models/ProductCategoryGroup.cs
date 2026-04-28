namespace Shop.Maui.Models;

public sealed record ProductCategoryGroup(
    ProductCategoryOption PrimaryCategory,
    IReadOnlyList<ProductCategoryOption> SecondaryCategories);
