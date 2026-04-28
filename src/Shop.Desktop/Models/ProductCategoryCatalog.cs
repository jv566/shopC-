using System.Collections.ObjectModel;

namespace Shop.Desktop.Models;

public static class ProductCategoryCatalog
{
    public static class PrimaryIds
    {
        public const string Bed = "bed";
        public const string Sofa = "sofa";
        public const string Table = "table";
        public const string Wardrobe = "wardrobe";
        public const string Custom = "custom";
    }

    private static readonly IReadOnlyList<ProductCategoryGroup> CategoryTree = new ReadOnlyCollection<ProductCategoryGroup>(
        new List<ProductCategoryGroup>
        {
            new(
                new ProductCategoryOption(PrimaryIds.Bed, "床"),
                new ReadOnlyCollection<ProductCategoryOption>(
                    new List<ProductCategoryOption>
                    {
                        new("bed-wood", "木板床"),
                        new("bed-leather", "真皮床"),
                        new("bed-fabric", "布艺床")
                    })),
            new(
                new ProductCategoryOption(PrimaryIds.Sofa, "沙发"),
                new ReadOnlyCollection<ProductCategoryOption>(
                    new List<ProductCategoryOption>
                    {
                        new("sofa-leather", "真皮沙发"),
                        new("sofa-fabric", "布艺沙发"),
                        new("sofa-corner", "转角沙发")
                    })),
            new(
                new ProductCategoryOption(PrimaryIds.Table, "桌子"),
                new ReadOnlyCollection<ProductCategoryOption>(
                    new List<ProductCategoryOption>
                    {
                        new("table-dining", "餐桌"),
                        new("table-coffee", "茶几"),
                        new("table-side", "边几")
                    })),
            new(
                new ProductCategoryOption(PrimaryIds.Wardrobe, "衣柜"),
                new ReadOnlyCollection<ProductCategoryOption>(
                    new List<ProductCategoryOption>
                    {
                        new("wardrobe-sliding", "推拉门衣柜"),
                        new("wardrobe-hinged", "平开门衣柜"),
                        new("wardrobe-custom", "定制衣柜")
                    })),
            new(
                new ProductCategoryOption(PrimaryIds.Custom, "定制柜"),
                new ReadOnlyCollection<ProductCategoryOption>(
                    new List<ProductCategoryOption>
                    {
                        new("custom-tv", "电视柜"),
                        new("custom-entry", "玄关柜"),
                        new("custom-cabinet", "多功能柜")
                    }))
        });

    private static readonly IReadOnlyList<ProductCategoryOption> PrimaryCategories = new ReadOnlyCollection<ProductCategoryOption>(
        CategoryTree.Select(x => x.PrimaryCategory).ToList());

    public static IReadOnlyList<ProductCategoryGroup> GetCategoryTree()
    {
        return CategoryTree;
    }

    public static IReadOnlyList<ProductCategoryOption> GetPrimaryCategories()
    {
        return PrimaryCategories;
    }

    public static ProductCategoryGroup? ResolvePrimaryGroup(string? categoryKey)
    {
        var key = (categoryKey ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(key))
        {
            return CategoryTree.FirstOrDefault();
        }

        var byPrimaryId = CategoryTree.FirstOrDefault(g => string.Equals(g.PrimaryCategory.Id, key, StringComparison.OrdinalIgnoreCase));
        if (byPrimaryId is not null)
        {
            return byPrimaryId;
        }

        var byPrimaryName = CategoryTree.FirstOrDefault(g => string.Equals(g.PrimaryCategory.DisplayName, key, StringComparison.OrdinalIgnoreCase));
        if (byPrimaryName is not null)
        {
            return byPrimaryName;
        }

        return CategoryTree.FirstOrDefault(g => g.SecondaryCategories.Any(s =>
            string.Equals(s.Id, key, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(s.DisplayName, key, StringComparison.OrdinalIgnoreCase)));
    }

    public static IReadOnlyList<ProductCategoryOption> GetSecondaryCategories(string primaryId)
    {
        var group = CategoryTree.FirstOrDefault(g => string.Equals(g.PrimaryCategory.Id, primaryId, StringComparison.OrdinalIgnoreCase));
        return group?.SecondaryCategories ?? Array.Empty<ProductCategoryOption>();
    }

    public static ProductCategoryOption? ResolveSecondaryCategory(string? categoryKey, string primaryId)
    {
        var secondaries = GetSecondaryCategories(primaryId);
        if (secondaries.Count == 0)
        {
            return null;
        }

        var key = (categoryKey ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(key))
        {
            var directHit = secondaries.FirstOrDefault(s =>
                string.Equals(s.Id, key, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s.DisplayName, key, StringComparison.OrdinalIgnoreCase));

            if (directHit is not null)
            {
                return directHit;
            }
        }

        return secondaries[0];
    }
}
