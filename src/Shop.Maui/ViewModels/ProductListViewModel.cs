using System.Collections.ObjectModel;
using System.Windows.Input;
using Shop.Maui.Models;
using Shop.Maui.Services;

namespace Shop.Maui.ViewModels;

public sealed class ProductListViewModel : ObservableObject, IQueryAttributable
{
    private readonly IProductProvider _productProvider;
    private readonly IProductCategoryTreeProvider _categoryTreeProvider;
    private readonly INavigationService _navigationService;

    private string _currentCategoryText = string.Empty;
    private string? _activePrimaryId;
    private ProductCategoryOption? _entryCategory;

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

    public ICommand SelectPrimaryCategoryCommand { get; }
    public ICommand SelectSecondaryCategoryCommand { get; }
    public ICommand SelectProductCommand { get; }

    public ProductListViewModel(
        IProductProvider productProvider,
        IProductCategoryTreeProvider categoryTreeProvider,
        INavigationService navigationService)
    {
        _productProvider = productProvider;
        _categoryTreeProvider = categoryTreeProvider;
        _navigationService = navigationService;

        SelectPrimaryCategoryCommand = new Command<ProductCategoryOption>(async primary =>
        {
            if (primary is not null)
            {
                await SelectPrimaryCategoryAsync(primary);
            }
        });

        SelectSecondaryCategoryCommand = new Command<ProductCategoryOption>(async secondary =>
        {
            if (secondary is not null)
            {
                await SelectSecondaryCategoryAsync(secondary);
            }
        });

        SelectProductCommand = new Command<ProductListItem>(async product =>
        {
            if (product is not null)
            {
                await _navigationService.GoToProductDetailAsync(product.Id, product.ModelName, product.SalePrice, product.ImageUrl);
            }
        });
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var categoryId = query.TryGetValue("categoryId", out var cid) ? cid as string : null;
        var categoryName = query.TryGetValue("categoryName", out var cname) ? cname as string : null;

        if (!string.IsNullOrWhiteSpace(categoryId) && !string.IsNullOrWhiteSpace(categoryName))
        {
            _entryCategory = new ProductCategoryOption(categoryId, categoryName);
            CurrentCategoryText = $"当前分类：{categoryName}";
        }
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var categoryTree = await _categoryTreeProvider.GetCategoryTreeAsync(cancellationToken);
        ReplaceCollection(CategoryTree, categoryTree);

        if (_entryCategory is null)
        {
            ReplaceCollection(Products, Array.Empty<ProductListItem>());
            return;
        }

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
        if (string.IsNullOrWhiteSpace(candidate) || _entryCategory is null)
        {
            return false;
        }

        return string.Equals(candidate, _entryCategory.Id, StringComparison.OrdinalIgnoreCase)
            || string.Equals(candidate, _entryCategory.DisplayName, StringComparison.OrdinalIgnoreCase);
    }
}
