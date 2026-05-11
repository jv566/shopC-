using System.Collections.ObjectModel;

namespace Shop.Maui.Models;

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
                        new("12", "真皮床"),
                        new("13", "植物皮床"),
                        new("14", "布艺床"),
                        new("15", "实木硬靠床"),
                        new("16", "实木软靠床")
                    })),
            new(
                new ProductCategoryOption(PrimaryIds.Sofa, "沙发"),
                new ReadOnlyCollection<ProductCategoryOption>(
                    new List<ProductCategoryOption>
                    {
                        new("17", "真皮沙发"),
                        new("18", "植物皮沙发"),
                        new("19", "布艺沙发"),
                        new("20", "新中式沙发"),
                        new("21", "乌金木沙发"),
                        new("22", "办公沙发")
                    })),
            new(
                new ProductCategoryOption(PrimaryIds.Table, "桌子"),
                new ReadOnlyCollection<ProductCategoryOption>(
                    new List<ProductCategoryOption>
                    {
                        new("23", "西餐桌"),
                        new("24", "实木西餐桌"),
                        new("26", "石面西餐桌"),
                        new("27", "跳台餐桌"),
                        new("30", "实木跳台餐桌"),
                        new("31", "石面跳台餐桌"),
                        new("32", "圆餐桌"),
                        new("33", "普通餐椅"),
                        new("34", "实木餐椅")
                    })),
            new(
                new ProductCategoryOption(PrimaryIds.Wardrobe, "衣柜"),
                new ReadOnlyCollection<ProductCategoryOption>(
                    new List<ProductCategoryOption>
                    {
                        new("35", "衣柜"),
                        new("36", "边柜"),
                        new("37", "鞋柜"),
                        new("38", "玄关柜"),
                        new("39", "书柜"),
                        new("40", "博古架")
                    })),
            new(
                new ProductCategoryOption(PrimaryIds.Custom, "定制柜"),
                new ReadOnlyCollection<ProductCategoryOption>(
                    new List<ProductCategoryOption>
                    {
                        new("56", "梳妆台"),
                        new("57", "梳妆凳"),
                        new("58", "书桌")
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

    public static ProductCategoryOption? ResolveSecondaryCategory(string? secondaryKey, string primaryId)
    {
        var key = (secondaryKey ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        var group = CategoryTree.FirstOrDefault(g => string.Equals(g.PrimaryCategory.Id, primaryId, StringComparison.OrdinalIgnoreCase));
        if (group is null)
        {
            return null;
        }

        return group.SecondaryCategories.FirstOrDefault(s =>
            string.Equals(s.Id, key, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(s.DisplayName, key, StringComparison.OrdinalIgnoreCase));
    }

    public static IReadOnlyList<ProductCategoryOption> GetSecondaryCategories(string primaryId)
    {
        var group = CategoryTree.FirstOrDefault(g => string.Equals(g.PrimaryCategory.Id, primaryId, StringComparison.OrdinalIgnoreCase));
        return group?.SecondaryCategories ?? Array.Empty<ProductCategoryOption>();
    }
}
