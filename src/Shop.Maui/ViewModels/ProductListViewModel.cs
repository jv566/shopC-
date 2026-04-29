using System.Collections.ObjectModel;
using System.Windows.Input;
using Shop.Maui.Models;
using Shop.Maui.Services;

namespace Shop.Maui.ViewModels;

public sealed class ProductListViewModel : ObservableObject, IQueryAttributable
{
    private const string BedImageSource = "image2/product_bed.png";
    private const string DefaultImageSource = "image2/product_bed.png";

    private readonly IProductProvider _productProvider;
    private readonly IProductCategoryTreeProvider _categoryTreeProvider;
    private readonly INavigationService _navigationService;

    private string _currentCategoryText = string.Empty;
    private string? _activePrimaryId;
    private string? _activeSecondaryId;
    private ProductCategoryOption? _entryCategory;
    private ProductListPrimaryMenuItem? _selectedPrimaryMenu;
    private bool _isInitialized;

    public ObservableCollection<ProductCategoryGroup> CategoryTree { get; } = [];

    public ObservableCollection<ProductListPrimaryMenuItem> PrimaryMenus { get; } = [];

    public ObservableCollection<ProductListPrimaryMenuItem> OtherPrimaryMenus { get; } = [];

    public ObservableCollection<ProductListSecondaryMenuItem> SecondaryMenus { get; } = [];

    public ObservableCollection<ProductListDisplayItem> DisplayProducts { get; } = [];

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

    public string? ActiveSecondaryId
    {
        get => _activeSecondaryId;
        set => SetProperty(ref _activeSecondaryId, value);
    }

    public ProductListPrimaryMenuItem? SelectedPrimaryMenu
    {
        get => _selectedPrimaryMenu;
        private set => SetProperty(ref _selectedPrimaryMenu, value);
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
        if (_isInitialized) return;

        var categoryTree = await _categoryTreeProvider.GetCategoryTreeAsync(cancellationToken);
        ReplaceCollection(CategoryTree, categoryTree);

        if (_entryCategory is null)
        {
            ReplaceCollection(DisplayProducts, Array.Empty<ProductListDisplayItem>());
            _isInitialized = true;
            return;
        }

        ReplaceCollection(PrimaryMenus, CategoryTree.Select(group => new ProductListPrimaryMenuItem(group)));

        var resolvedGroup = ProductCategoryCatalog.ResolvePrimaryGroup(_entryCategory.Id)
            ?? ProductCategoryCatalog.ResolvePrimaryGroup(_entryCategory.DisplayName);

        var targetGroup = resolvedGroup is null
            ? CategoryTree.FirstOrDefault()
            : CategoryTree.FirstOrDefault(g => string.Equals(g.PrimaryCategory.Id, resolvedGroup.PrimaryCategory.Id, StringComparison.OrdinalIgnoreCase));

        if (targetGroup is null)
        {
            ReplaceCollection(DisplayProducts, Array.Empty<ProductListDisplayItem>());
            _isInitialized = true;
            return;
        }

        var matchedSecondary = targetGroup.SecondaryCategories.FirstOrDefault(s => IsEntryMatch(s.Id) || IsEntryMatch(s.DisplayName));

        if (matchedSecondary is not null)
        {
            SetPrimarySelection(targetGroup.PrimaryCategory.Id);
            RefreshSecondaryMenus(targetGroup.SecondaryCategories, matchedSecondary.Id);
            await SelectSecondaryCategoryAsync(matchedSecondary, cancellationToken);
            _isInitialized = true;
            return;
        }

        await SelectPrimaryCategoryAsync(targetGroup.PrimaryCategory, cancellationToken);

        _isInitialized = true;
    }

    public async Task SelectPrimaryCategoryAsync(ProductCategoryOption primaryCategory, CancellationToken cancellationToken = default)
    {
        CurrentCategoryText = $"当前分类：{primaryCategory.DisplayName}（全部）";
        ActivePrimaryId = primaryCategory.Id;
        ActiveSecondaryId = null;

        SetPrimarySelection(primaryCategory.Id);

        var targetGroup = CategoryTree.FirstOrDefault(g => string.Equals(g.PrimaryCategory.Id, primaryCategory.Id, StringComparison.OrdinalIgnoreCase));
        RefreshSecondaryMenus(targetGroup?.SecondaryCategories ?? Array.Empty<ProductCategoryOption>(), null);

        var products = await _productProvider.GetProductsAsync(primaryCategory.Id, cancellationToken);
        ReplaceCollection(DisplayProducts, BuildDisplayProducts(products, primaryCategory.Id));
    }

    public async Task SelectSecondaryCategoryAsync(ProductCategoryOption secondaryCategory, CancellationToken cancellationToken = default)
    {
        CurrentCategoryText = $"当前分类：{secondaryCategory.DisplayName}";
        ActiveSecondaryId = secondaryCategory.Id;

        var targetGroup = CategoryTree.FirstOrDefault(g =>
            g.SecondaryCategories.Any(s => string.Equals(s.Id, secondaryCategory.Id, StringComparison.OrdinalIgnoreCase)));

        if (targetGroup is not null)
        {
            ActivePrimaryId = targetGroup.PrimaryCategory.Id;
            SetPrimarySelection(targetGroup.PrimaryCategory.Id);
            RefreshSecondaryMenus(targetGroup.SecondaryCategories, secondaryCategory.Id);
        }

        var products = await _productProvider.GetProductsAsync(secondaryCategory.Id, cancellationToken);
        ReplaceCollection(DisplayProducts, BuildDisplayProducts(products, ActivePrimaryId));
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

    private void SetPrimarySelection(string? primaryId)
    {
        ProductListPrimaryMenuItem? selected = null;
        foreach (var menu in PrimaryMenus)
        {
            var isSelected = string.Equals(menu.Id, primaryId, StringComparison.OrdinalIgnoreCase);
            menu.IsSelected = isSelected;
            if (isSelected)
            {
                selected = menu;
            }
        }

        SelectedPrimaryMenu = selected;
        ReplaceCollection(OtherPrimaryMenus, PrimaryMenus.Where(menu => !ReferenceEquals(menu, selected)));
    }

    private void RefreshSecondaryMenus(IEnumerable<ProductCategoryOption> secondaryCategories, string? selectedSecondaryId)
    {
        var items = secondaryCategories.Select(category =>
        {
            var item = new ProductListSecondaryMenuItem(category)
            {
                IsSelected = string.Equals(category.Id, selectedSecondaryId, StringComparison.OrdinalIgnoreCase)
            };
            return item;
        });

        ReplaceCollection(SecondaryMenus, items);
    }

    private static IReadOnlyList<ProductListDisplayItem> BuildDisplayProducts(IEnumerable<ProductListItem> products, string? primaryCategoryId)
    {
        var source = products.ToList();
        if (source.Count == 0)
        {
            return Array.Empty<ProductListDisplayItem>();
        }

        var result = new List<ProductListDisplayItem>();
        for (var i = 0; i < 6; i++)
        {
            var product = source[i % source.Count];
            result.Add(new ProductListDisplayItem(
                product,
                ResolveImageSource(primaryCategoryId, product),
                $"￥{product.SalePrice:F0}",
                BuildDisplayModelText(product),
                "image2/label_model.png",
                "image2/label_price.png",
                "image2/card_panel.png"));
        }

        return result;
    }

    private static string ResolveImageSource(string? primaryCategoryId, ProductListItem product)
    {
        if (!string.IsNullOrWhiteSpace(product.ImageUrl))
        {
            return product.ImageUrl;
        }

        return string.Equals(primaryCategoryId, ProductCategoryCatalog.PrimaryIds.Bed, StringComparison.OrdinalIgnoreCase)
            ? BedImageSource
            : DefaultImageSource;
    }

    private static string BuildDisplayModelText(ProductListItem product)
    {
        return product.ModelName.StartsWith("型号", StringComparison.OrdinalIgnoreCase)
            ? product.ModelName
            : $"型号{product.ModelName}";
    }
}
