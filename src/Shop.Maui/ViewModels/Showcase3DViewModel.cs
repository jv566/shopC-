using System.Collections.ObjectModel;
using System.Windows.Input;
using Shop.Maui.Models;
using Shop.Maui.Services;

namespace Shop.Maui.ViewModels;

public sealed class Showcase3DViewModel : ObservableObject
{
    private const string DefaultImageSource = "sofa_preview.png";

    private readonly IProductProvider _productProvider;
    private readonly IUserActionService _userActionService;
    private bool _isInitialized;

    private ShowcaseCategoryItem? _selectedCategory;
    private ShowcaseProductThumbnail? _selectedThumbnail;
    private string _selectedProductName = string.Empty;
    private string _selectedCategoryName = string.Empty;
    private string _selectedProductPriceText = string.Empty;
    private string _selectedProductImageSource = DefaultImageSource;

    public Showcase3DViewModel(IProductProvider productProvider, IUserActionService userActionService)
    {
        _productProvider = productProvider;
        _userActionService = userActionService;

        Categories = [];
        Thumbnails = [];

        SelectCategoryCommand = new Command<ShowcaseCategoryItem>(async category =>
        {
            if (category is not null)
            {
                await SelectCategoryAsync(category);
            }
        });

        SelectThumbnailCommand = new Command<ShowcaseProductThumbnail>(thumbnail =>
        {
            if (thumbnail is not null)
            {
                SelectThumbnail(thumbnail);
            }
        });

        AddToCartCommand = new Command(async () => await AddSelectedProductToCartAsync());
    }

    public ObservableCollection<ShowcaseCategoryItem> Categories { get; }

    public ObservableCollection<ShowcaseProductThumbnail> Thumbnails { get; }

    public string SelectedProductName
    {
        get => _selectedProductName;
        private set => SetProperty(ref _selectedProductName, value);
    }

    public string SelectedCategoryName
    {
        get => _selectedCategoryName;
        private set => SetProperty(ref _selectedCategoryName, value);
    }

    public string SelectedProductPriceText
    {
        get => _selectedProductPriceText;
        private set => SetProperty(ref _selectedProductPriceText, value);
    }

    public string SelectedProductImageSource
    {
        get => _selectedProductImageSource;
        private set => SetProperty(ref _selectedProductImageSource, value);
    }

    public ICommand SelectCategoryCommand { get; }

    public ICommand SelectThumbnailCommand { get; }

    public ICommand AddToCartCommand { get; }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
        {
            return;
        }

        var categories = ProductCategoryCatalog
            .GetPrimaryCategories()
            .Take(3)
            .Select(x => new ShowcaseCategoryItem(x.Id, x.DisplayName))
            .ToList();

        ReplaceCollection(Categories, categories);

        var defaultCategory = Categories.FirstOrDefault();
        if (defaultCategory is not null)
        {
            await SelectCategoryAsync(defaultCategory, cancellationToken);
        }

        _isInitialized = true;
    }

    public async Task SelectCategoryAsync(ShowcaseCategoryItem category, CancellationToken cancellationToken = default)
    {
        _selectedCategory = category;

        foreach (var item in Categories)
        {
            item.IsSelected = ReferenceEquals(item, category);
        }

        SelectedCategoryName = category.DisplayName;

        var products = await _productProvider.GetProductsAsync(category.Id, cancellationToken);
        var thumbnails = BuildThumbnailList(products);
        ReplaceCollection(Thumbnails, thumbnails);

        var firstThumbnail = Thumbnails.FirstOrDefault();
        if (firstThumbnail is not null)
        {
            SelectThumbnail(firstThumbnail);
        }
    }

    public void SelectThumbnail(ShowcaseProductThumbnail thumbnail)
    {
        _selectedThumbnail = thumbnail;

        foreach (var item in Thumbnails)
        {
            item.IsSelected = ReferenceEquals(item, thumbnail);
        }

        SelectedProductName = thumbnail.Product.ModelName;
        SelectedProductPriceText = $"￥{thumbnail.Product.SalePrice:F2}";
        SelectedProductImageSource = thumbnail.ImageSource;
    }

    private async Task AddSelectedProductToCartAsync()
    {
        if (_selectedThumbnail is null)
        {
            await ShowAlertAsync(new UserActionResult("购物车", "当前没有可加入购物车的商品。"));
            return;
        }

        var result = await _userActionService.AddToCartAsync(_selectedThumbnail.Product);
        await ShowAlertAsync(result);
    }

    private static IReadOnlyList<ShowcaseProductThumbnail> BuildThumbnailList(IReadOnlyList<ProductListItem> products)
    {
        if (products.Count == 0)
        {
            return Array.Empty<ShowcaseProductThumbnail>();
        }

        var list = new List<ShowcaseProductThumbnail>();
        for (var i = 0; i < 8; i++)
        {
            var product = products[i % products.Count];
            list.Add(new ShowcaseProductThumbnail(product, ResolveImageSource(product)));
        }

        return list;
    }

    private static string ResolveImageSource(ProductListItem product)
    {
        return string.IsNullOrWhiteSpace(product.ImageUrl) ? DefaultImageSource : product.ImageUrl;
    }

    private static async Task ShowAlertAsync(UserActionResult result)
    {
        if (Shell.Current is not null)
        {
            await Shell.Current.DisplayAlert(result.Title, result.Message, "确定");
        }
    }

    private static void ReplaceCollection<T>(ObservableCollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }
}
