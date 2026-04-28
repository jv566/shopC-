using System.Collections.ObjectModel;
using Shop.Desktop.Models;
using Shop.Desktop.Services;

namespace Shop.Desktop.ViewModels;

public sealed class ProductListViewModel : ObservableObject
{
    private readonly ProductCategoryOption _entryCategory;
    private readonly IProductProvider _productProvider;
    private readonly IProductCategoryTreeProvider _categoryTreeProvider;

    private string _currentCategoryText;
    private string? _activePrimaryId;

    public ProductListViewModel(
        ProductCategoryOption entryCategory,
        IProductProvider productProvider,
        IProductCategoryTreeProvider categoryTreeProvider)
    {
        _entryCategory = entryCategory;
        _productProvider = productProvider;
        _categoryTreeProvider = categoryTreeProvider;
        _currentCategoryText = $"当前分类：{entryCategory.DisplayName}";
    }

    public ObservableCollection<ProductCategoryGroup> CategoryTree { get; } = [];

    public ObservableCollection<ProductListItem> Products { get; } = [];

    public string CurrentCategoryText
    {
        get => _currentCategoryText;
        private set => SetProperty(ref _currentCategoryText, value);
    }

    public string? ActivePrimaryId
    {
        get => _activePrimaryId;
        set => SetProperty(ref _activePrimaryId, value);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var categoryTree = await _categoryTreeProvider.GetCategoryTreeAsync(cancellationToken);
        ReplaceCollection(CategoryTree, categoryTree);

        var resolvedGroup = ProductCategoryCatalog.ResolvePrimaryGroup(_entryCategory.Id)
            ?? ProductCategoryCatalog.ResolvePrimaryGroup(_entryCategory.DisplayName);

        var targetGroup = resolvedGroup is null
            ? CategoryTree.FirstOrDefault()
            : CategoryTree.FirstOrDefault(g => string.Equals(g.PrimaryCategory.Id, resolvedGroup.PrimaryCategory.Id, StringComparison.OrdinalIgnoreCase));

        if (targetGroup is null)
        {
            ReplaceCollection(Products, Array.Empty<ProductListItem>());
            return;
        }

        ActivePrimaryId = targetGroup.PrimaryCategory.Id;

        var matchedSecondary = targetGroup.SecondaryCategories.FirstOrDefault(s => IsEntryMatch(s.Id) || IsEntryMatch(s.DisplayName));

        if (matchedSecondary is not null)
        {
            await SelectSecondaryCategoryAsync(matchedSecondary, cancellationToken);
            return;
        }

        await SelectPrimaryCategoryAsync(targetGroup.PrimaryCategory, cancellationToken);
    }

    public async Task SelectPrimaryCategoryAsync(ProductCategoryOption primaryCategory, CancellationToken cancellationToken = default)
    {
        CurrentCategoryText = $"当前分类：{primaryCategory.DisplayName}（全部）";

        var products = await _productProvider.GetProductsAsync(primaryCategory.Id, cancellationToken);
        ReplaceCollection(Products, products);
    }

    public async Task SelectSecondaryCategoryAsync(ProductCategoryOption secondaryCategory, CancellationToken cancellationToken = default)
    {
        CurrentCategoryText = $"当前分类：{secondaryCategory.DisplayName}";

        var products = await _productProvider.GetProductsAsync(secondaryCategory.Id, cancellationToken);
        ReplaceCollection(Products, products);
    }

    private bool IsEntryMatch(string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        return string.Equals(candidate, _entryCategory.Id, StringComparison.OrdinalIgnoreCase)
            || string.Equals(candidate, _entryCategory.DisplayName, StringComparison.OrdinalIgnoreCase);
    }

    private static void ReplaceCollection<T>(ICollection<T> target, IEnumerable<T> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }
    }
}
