using System.Collections.ObjectModel; // ObservableCollection：集合变化后能通知界面刷新
using System.Windows.Input;           // ICommand：命令接口，用于按钮点击、列表点击等绑定
using Shop.Maui.Models;               // 引入 Models 里的商品、分类、菜单项等模型
using Shop.Maui.Services;             // 引入服务，比如获取商品、获取分类树、页面跳转

namespace Shop.Maui.ViewModels;

// 商品列表页的 ViewModel
// ViewModel 的作用：给页面提供数据和命令
// 页面 XAML 一般会绑定这里的属性和 Command
public sealed class ProductListViewModel : ObservableObject, IQueryAttributable
{
    // 床的默认图片
    private const string BedImageSource = "product_bed.png";

    // 默认商品图片
    private const string DefaultImageSource = "product_bed.png";
    private const int ProductPlaceholderCount = 9;

    // 商品数据服务：负责根据分类 id 获取商品列表
    private readonly IProductProvider _productProvider;

    // 分类树服务：负责获取一级分类、二级分类
    private readonly IProductCategoryTreeProvider _categoryTreeProvider;

    // 导航服务：负责页面跳转，比如跳到商品详情页
    private readonly INavigationService _navigationService;

    private readonly IImageCacheService _imageCacheService;

    // 当前页面顶部/标题显示的分类文字
    private string _currentCategoryText = string.Empty;

    // 当前选中的一级分类 id
    private string? _activePrimaryId;

    // 当前选中的二级分类 id
    private string? _activeSecondaryId;

    // 进入页面时传进来的分类
    // 比如从首页点击“床”进入商品列表页
    private ProductCategoryOption? _entryCategory;

    // 当前选中的左侧一级菜单项
    private ProductListPrimaryMenuItem? _selectedPrimaryMenu;

    // 防止 InitializeAsync 重复执行
    private bool _isInitialized;
    private bool _isPagePreloadStarted;
    private int _productLoadVersion;
    private CancellationTokenSource? _productLoadCts;

    // 分类树数据
    // ObservableCollection 适合绑定到界面，添加/删除元素时界面会刷新
    public ObservableCollection<ProductCategoryGroup> CategoryTree { get; } = [];

    // 左侧所有一级菜单
    public ObservableCollection<ProductListPrimaryMenuItem> PrimaryMenus { get; } = [];

    // 除了当前选中项以外的其他一级菜单
    // 可能用于界面布局：选中的放上面，其他的放下面
    public ObservableCollection<ProductListPrimaryMenuItem> OtherPrimaryMenus { get; } = [];

    // 二级分类菜单
    public ObservableCollection<ProductListSecondaryMenuItem> SecondaryMenus { get; } = [];

    // 页面上展示的商品卡片列表
    public ObservableCollection<ProductListDisplayItem> DisplayProducts { get; } = [];

    // 当前分类显示文字
    public string CurrentCategoryText
    {
        get => _currentCategoryText;

        // private set：外部只能读，只有 ViewModel 内部能改
        // SetProperty 是 ObservableObject 里的方法
        // 作用：修改字段，并通知界面属性变化
        private set => SetProperty(ref _currentCategoryText, value);
    }

    // 当前选中的一级分类 id
    public string? ActivePrimaryId
    {
        get => _activePrimaryId;
        set => SetProperty(ref _activePrimaryId, value);
    }

    // 当前选中的二级分类 id
    public string? ActiveSecondaryId
    {
        get => _activeSecondaryId;
        set => SetProperty(ref _activeSecondaryId, value);
    }

    // 当前选中的一级菜单项
    public ProductListPrimaryMenuItem? SelectedPrimaryMenu
    {
        get => _selectedPrimaryMenu;
        private set => SetProperty(ref _selectedPrimaryMenu, value);
    }

    // 点击一级分类时执行的命令
    public ICommand SelectPrimaryCategoryCommand { get; }

    // 点击二级分类时执行的命令
    public ICommand SelectSecondaryCategoryCommand { get; }

    // 点击商品时执行的命令
    public ICommand SelectProductCommand { get; }

    // 构造方法
    // 通过依赖注入传入三个服务
    public ProductListViewModel(
        IProductProvider productProvider,
        IProductCategoryTreeProvider categoryTreeProvider,
        INavigationService navigationService,
        IImageCacheService imageCacheService)
    {
        _productProvider = productProvider;
        _categoryTreeProvider = categoryTreeProvider;
        _navigationService = navigationService;
        _imageCacheService = imageCacheService;

        // 一级分类点击命令
        SelectPrimaryCategoryCommand = new Command<ProductCategoryOption>(async primary =>
        {
            // 防止参数为空
            if (primary is not null)
            {
                // 选择一级分类
                await SelectPrimaryCategoryAsync(primary);
            }
        });

        // 二级分类点击命令
        SelectSecondaryCategoryCommand = new Command<ProductCategoryOption>(async secondary =>
        {
            if (secondary is not null)
            {
                // 选择二级分类
                await SelectSecondaryCategoryAsync(secondary);
            }
        });

        // 商品点击命令
        SelectProductCommand = new Command<ProductListItem>(async product =>
        {
            if (product is not null && !string.IsNullOrWhiteSpace(product.Id))
            {
                // 跳转到商品详情页
                // 传入商品 id、型号、价格、图片
                await _navigationService.GoToProductDetailAsync(
                    product.Id,
                    product.ModelName,
                    product.SalePrice,
                    product.ImageUrl);
            }
        });
    }

    // 接收页面跳转传过来的参数
    // 比如 Shell 跳转时传 categoryId、categoryName
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        // 从 query 里取 categoryId
        var categoryId = query.TryGetValue("categoryId", out var cid)
            ? cid as string
            : null;

        // 从 query 里取 categoryName
        var categoryName = query.TryGetValue("categoryName", out var cname)
            ? cname as string
            : null;

        // 如果 id 和 name 都有值
        if (!string.IsNullOrWhiteSpace(categoryId) &&
            !string.IsNullOrWhiteSpace(categoryName))
        {
            // 保存入口分类
            // 说明用户是从某个分类入口进来的
            _entryCategory = new ProductCategoryOption(categoryId, categoryName);

            // 更新页面显示文字
            CurrentCategoryText = $"当前分类：{categoryName}";
        }
    }

    // 初始化页面数据
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // 如果已经初始化过，直接返回
        // 防止页面多次出现时重复加载
        if (_isInitialized) return;

        // 获取分类树
        var categoryTree = await _categoryTreeProvider.GetCategoryTreeAsync(cancellationToken);

        // 替换 CategoryTree 集合内容
        ReplaceCollection(CategoryTree, categoryTree);

        // 如果没有入口分类，说明不知道要显示哪个分类
        if (_entryCategory is null)
        {
            // 清空商品列表
            ReplaceCollection(DisplayProducts, Array.Empty<ProductListDisplayItem>());

            _isInitialized = true;
            return;
        }

        // 根据分类树生成左侧一级菜单
        ReplaceCollection(
            PrimaryMenus,
            CategoryTree.Select(group => new ProductListPrimaryMenuItem(group)));

        // 找到入口分类对应的一级分类组
        // 先用 id 找，再用显示名找，都找不到就默认第一个分类
        var targetGroup =
            ResolveLoadedPrimaryGroup(_entryCategory.Id)
            ?? ResolveLoadedPrimaryGroup(_entryCategory.DisplayName)
            ?? CategoryTree.FirstOrDefault();

        // 如果分类树为空，找不到目标分类
        if (targetGroup is null)
        {
            ReplaceCollection(DisplayProducts, Array.Empty<ProductListDisplayItem>());
            _isInitialized = true;
            return;
        }

        // 判断入口分类是不是某个二级分类
        var matchedSecondary = targetGroup.SecondaryCategories.FirstOrDefault(s =>
            IsEntryMatch(s.Id) || IsEntryMatch(s.DisplayName));

        // 如果匹配到了二级分类
        if (matchedSecondary is not null)
        {
            // 设置一级菜单选中
            SetPrimarySelection(targetGroup.PrimaryCategory.Id);

            // 刷新二级菜单，并选中对应二级分类
            RefreshSecondaryMenus(targetGroup.SecondaryCategories, matchedSecondary.Id);

            // 加载该二级分类下的商品
            await SelectSecondaryCategoryAsync(matchedSecondary, cancellationToken);

            _isInitialized = true;
            StartPageDataPreload();
            return;
        }

        // 如果入口分类不是二级分类，就按一级分类加载
        await SelectPrimaryCategoryAsync(targetGroup.PrimaryCategory, cancellationToken);

        _isInitialized = true;
        StartPageDataPreload();
    }

    // 选择一级分类
    public async Task SelectPrimaryCategoryAsync(
        ProductCategoryOption primaryCategory,
        CancellationToken cancellationToken = default)
    {
        if (IsPrimaryMenuExpanded(primaryCategory.Id))
        {
            CollapsePrimaryMenu();
            return;
        }

        // 顶部显示：当前分类：xxx（全部）
        CurrentCategoryText = $"当前分类：{primaryCategory.DisplayName}（全部）";

        // 当前一级分类 id
        ActivePrimaryId = primaryCategory.Id;

        // 设置左侧一级菜单选中状态
        SetPrimarySelection(primaryCategory.Id);

        // 找到对应分类组
        var targetGroup = CategoryTree.FirstOrDefault(g =>
            string.Equals(
                g.PrimaryCategory.Id,
                primaryCategory.Id,
                StringComparison.OrdinalIgnoreCase));

        var defaultSecondary = targetGroup?.SecondaryCategories.FirstOrDefault();
        if (defaultSecondary is not null)
        {
            RefreshSecondaryMenus(targetGroup!.SecondaryCategories, defaultSecondary.Id);
            await SelectSecondaryCategoryAsync(defaultSecondary, cancellationToken);
            return;
        }

        // 刷新二级菜单
        // 如果找不到对应组，就显示空二级菜单
        RefreshSecondaryMenus(
            targetGroup?.SecondaryCategories ?? Array.Empty<ProductCategoryOption>(),
            null);

        ActiveSecondaryId = null;

        // 根据一级分类 id 获取商品
        ShowProductPlaceholders();

        var products = await _productProvider.GetProductsAsync(
            primaryCategory.Id,
            cancellationToken);

        // 构建商品展示卡片，并刷新页面商品列表
        ReplaceCollection(
            DisplayProducts,
            await BuildDisplayProductsAsync(products, primaryCategory.Id, cancellationToken));
    }

    // 选择二级分类
    public async Task SelectSecondaryCategoryAsync(
        ProductCategoryOption secondaryCategory,
        CancellationToken cancellationToken = default)
    {
        var loadToken = BeginProductLoad(cancellationToken, out var loadVersion);
        ShowProductPlaceholders();

        // 顶部显示当前二级分类名称
        CurrentCategoryText = $"当前分类：{secondaryCategory.DisplayName}";

        // 当前二级分类 id
        ActiveSecondaryId = secondaryCategory.Id;

        // 找到这个二级分类属于哪个一级分类组
        var targetGroup = CategoryTree.FirstOrDefault(g =>
            g.SecondaryCategories.Any(s =>
                string.Equals(
                    s.Id,
                    secondaryCategory.Id,
                    StringComparison.OrdinalIgnoreCase)));

        // 如果找到了所属一级分类
        if (targetGroup is not null)
        {
            // 设置当前一级分类 id
            ActivePrimaryId = targetGroup.PrimaryCategory.Id;

            // 设置左侧一级菜单选中状态
            SetPrimarySelection(targetGroup.PrimaryCategory.Id);

            // 刷新二级菜单，并选中当前二级分类
            RefreshSecondaryMenus(
                targetGroup.SecondaryCategories,
                secondaryCategory.Id);
        }

        // 根据二级分类 id 获取商品
        var products = await _productProvider.GetProductsAsync(
            secondaryCategory.Id,
            loadToken);

        if (!IsCurrentProductLoad(loadVersion, loadToken))
        {
            return;
        }

        // 构建商品展示卡片
        var displayProducts = await BuildDisplayProductsAsync(products, ActivePrimaryId, loadToken);
        if (!IsCurrentProductLoad(loadVersion, loadToken))
        {
            return;
        }

        ReplaceCollection(DisplayProducts, displayProducts);

        if (targetGroup is not null)
        {
            PrefetchSecondaryProducts(targetGroup.SecondaryCategories, secondaryCategory.Id);
        }
    }

    // 判断入口分类是否和某个候选值匹配
    private bool IsEntryMatch(string candidate)
    {
        // candidate 为空，或者入口分类为空，都返回 false
        if (string.IsNullOrWhiteSpace(candidate) || _entryCategory is null)
        {
            return false;
        }

        // candidate 可以匹配入口分类 id，也可以匹配入口分类名称
        return string.Equals(candidate, _entryCategory.Id, StringComparison.OrdinalIgnoreCase)
            || string.Equals(candidate, _entryCategory.DisplayName, StringComparison.OrdinalIgnoreCase);
    }

    // 根据 categoryKey 找到对应的一级分类组
    // categoryKey 可能是一级分类 id、一级分类名、二级分类 id、二级分类名
    private ProductCategoryGroup? ResolveLoadedPrimaryGroup(string? categoryKey)
    {
        var key = (categoryKey ?? string.Empty).Trim();

        // key 为空就返回 null
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        // 在分类树里查找
        return CategoryTree.FirstOrDefault(g =>
            // 匹配一级分类 id
            string.Equals(g.PrimaryCategory.Id, key, StringComparison.OrdinalIgnoreCase) ||

            // 匹配一级分类显示名
            string.Equals(g.PrimaryCategory.DisplayName, key, StringComparison.OrdinalIgnoreCase) ||

            // 匹配二级分类 id 或显示名
            g.SecondaryCategories.Any(s =>
                string.Equals(s.Id, key, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s.DisplayName, key, StringComparison.OrdinalIgnoreCase)));
    }

    // 设置一级菜单选中状态
    private void SetPrimarySelection(string? primaryId)
    {
        ProductListPrimaryMenuItem? selected = null;

        // 遍历所有一级菜单
        foreach (var menu in PrimaryMenus)
        {
            // 判断当前菜单是否等于传入的 primaryId
            var isSelected = string.Equals(
                menu.Id,
                primaryId,
                StringComparison.OrdinalIgnoreCase);

            // 设置菜单项是否选中
            // 这里会触发 ProductListPrimaryMenuItem 里的样式刷新
            menu.IsSelected = isSelected;

            // 保存当前选中的菜单项
            if (isSelected)
            {
                selected = menu;
            }
        }

        // 更新当前选中的一级菜单
        SelectedPrimaryMenu = selected;

        // OtherPrimaryMenus 保存除了选中项以外的菜单
        ReplaceCollection(
            OtherPrimaryMenus,
            PrimaryMenus.Where(menu => !ReferenceEquals(menu, selected)));
    }

    private bool IsPrimaryMenuExpanded(string? primaryId)
    {
        return SelectedPrimaryMenu is not null
            && SelectedPrimaryMenu.IsSelected
            && SecondaryMenus.Count > 0
            && string.Equals(SelectedPrimaryMenu.Id, primaryId, StringComparison.OrdinalIgnoreCase);
    }

    private void CollapsePrimaryMenu()
    {
        CancelCurrentProductLoad();
        SetPrimarySelection(null);
        RefreshSecondaryMenus(Array.Empty<ProductCategoryOption>(), null);
        ActiveSecondaryId = null;
    }

    // 刷新二级菜单
    private void RefreshSecondaryMenus(
        IEnumerable<ProductCategoryOption> secondaryCategories,
        string? selectedSecondaryId)
    {
        // 把 ProductCategoryOption 转成 ProductListSecondaryMenuItem
        var items = secondaryCategories.Select(category =>
        {
            var item = new ProductListSecondaryMenuItem(category)
            {
                // 判断这个二级分类是否被选中
                IsSelected = string.Equals(
                    category.Id,
                    selectedSecondaryId,
                    StringComparison.OrdinalIgnoreCase)
            };

            return item;
        });

        // 替换二级菜单集合
        ReplaceCollection(SecondaryMenus, items);
    }

    private CancellationToken BeginProductLoad(
        CancellationToken cancellationToken,
        out int loadVersion)
    {
        CancelCurrentProductLoad();
        _productLoadCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        loadVersion = ++_productLoadVersion;
        return _productLoadCts.Token;
    }

    private void CancelCurrentProductLoad()
    {
        _productLoadCts?.Cancel();
        _productLoadCts?.Dispose();
        _productLoadCts = null;
        _productLoadVersion++;
    }

    private bool IsCurrentProductLoad(int loadVersion, CancellationToken cancellationToken)
    {
        return loadVersion == _productLoadVersion && !cancellationToken.IsCancellationRequested;
    }

    private void PrefetchSecondaryProducts(
        IEnumerable<ProductCategoryOption> secondaryCategories,
        string selectedSecondaryId)
    {
        var nextCategories = secondaryCategories
            .Where(category => !string.Equals(category.Id, selectedSecondaryId, StringComparison.OrdinalIgnoreCase))
            .Select(category => category.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToList();

        if (nextCategories.Count == 0)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            using var throttler = new SemaphoreSlim(2);
            var tasks = nextCategories.Select(async categoryId =>
            {
                await throttler.WaitAsync().ConfigureAwait(false);

                try
                {
                    await _productProvider.GetProductsAsync(categoryId, CancellationToken.None).ConfigureAwait(false);
                }
                finally
                {
                    throttler.Release();
                }
            });

            try
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch
            {
            }
        });
    }

    // 构建页面展示用的商品卡片列表
    private void StartPageDataPreload()
    {
        if (_isPagePreloadStarted || CategoryTree.Count == 0)
        {
            return;
        }

        _isPagePreloadStarted = true;

        var categoryIds = CategoryTree
            .SelectMany(group => group.SecondaryCategories)
            .Select(category => category.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Where(id => !string.Equals(id, ActiveSecondaryId, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (categoryIds.Count == 0)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            using var throttler = new SemaphoreSlim(3);
            var tasks = categoryIds.Select(async categoryId =>
            {
                await throttler.WaitAsync().ConfigureAwait(false);

                try
                {
                    await _productProvider.GetProductsAsync(categoryId, CancellationToken.None).ConfigureAwait(false);
                }
                finally
                {
                    throttler.Release();
                }
            });

            try
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch
            {
            }
        });
    }

    private Task<IReadOnlyList<ProductListDisplayItem>> BuildDisplayProductsAsync(
        IEnumerable<ProductListItem> products,
        string? primaryCategoryId,
        CancellationToken cancellationToken)
    {
        // 先转成 List，避免重复枚举
        var source = products.ToList();

        // 如果没有商品，返回空数组
        if (source.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<ProductListDisplayItem>>(
                [CreateEmptyStateItem()]);
        }

        var result = new List<ProductListDisplayItem>(source.Count);

        foreach (var product in source)
        {
            var resolvedImageSource = ResolveImageSource(primaryCategoryId, product);

            // 创建展示商品对象
            result.Add(new ProductListDisplayItem(
                product,

                // 解析商品图片
                _imageCacheService.GetBestImageSource(resolvedImageSource),

                // 价格文本，比如 ￥1999
                $"￥ {product.SalePrice:F0}",

                // 型号文本
                BuildDisplayModelText(product),

                // 型号标签图片
                "label_model.png",

                // 价格标签图片
                "label_price.png",

                // 商品卡片背景图
                "card_panel.png"));
        }

        _ = RefreshCachedImagesAsync(result, primaryCategoryId, cancellationToken);

        return Task.FromResult<IReadOnlyList<ProductListDisplayItem>>(result);
    }

    private void ShowProductPlaceholders()
    {
        ReplaceCollection(
            DisplayProducts,
            Enumerable.Range(0, ProductPlaceholderCount)
                .Select(_ => new ProductListDisplayItem(
                    new ProductListItem(string.Empty, string.Empty, string.Empty, 0m, null),
                    DefaultImageSource,
                    string.Empty,
                    "...",
                    "label_model.png",
                    "label_price.png",
                    "card_panel.png",
                    true)));
    }

    private static ProductListDisplayItem CreateEmptyStateItem()
    {
        return new ProductListDisplayItem(
            new ProductListItem(string.Empty, string.Empty, string.Empty, 0m, null),
            DefaultImageSource,
            string.Empty,
            "无数据",
            "label_model.png",
            "label_price.png",
            "card_panel.png",
            isEmptyState: true);
    }

    private async Task RefreshCachedImagesAsync(
        IReadOnlyList<ProductListDisplayItem> items,
        string? primaryCategoryId,
        CancellationToken cancellationToken)
    {
        using var throttler = new SemaphoreSlim(4);

        var tasks = items.Select(async item =>
        {
            await throttler.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                var resolvedImageSource = ResolveImageSource(primaryCategoryId, item.Product);
                var cachedImageSource = await _imageCacheService
                    .GetCachedImageSourceAsync(resolvedImageSource, cancellationToken)
                    .ConfigureAwait(false);

                if (cachedImageSource == item.ImageSource)
                {
                    return;
                }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    item.ImageSource = cachedImageSource;
                });
            }
            finally
            {
                throttler.Release();
            }
        });

        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
    }

    // 解析商品图片
    private static string ResolveImageSource(
        string? primaryCategoryId,
        ProductListItem product)
    {
        // 如果商品自己有图片地址，就优先用商品图片
        if (!string.IsNullOrWhiteSpace(product.ImageUrl))
        {
            return product.ImageUrl;
        }

        // 如果商品没有图片：
        // 当前一级分类是床，就用床图片
        // 否则用默认图片
        return string.Equals(
            primaryCategoryId,
            ProductCategoryCatalog.PrimaryIds.Bed,
            StringComparison.OrdinalIgnoreCase)
            ? BedImageSource
            : DefaultImageSource;
    }

    // 构建型号显示文本
    private static string BuildDisplayModelText(ProductListItem product)
    {
        var modelName = product.ModelName.Trim();

        if (modelName.StartsWith("型号", StringComparison.OrdinalIgnoreCase))
        {
            modelName = modelName["型号".Length..].Trim();
        }

        var categoryEndIndex = modelName.IndexOf('】');
        if (modelName.StartsWith('【') && categoryEndIndex >= 0 && categoryEndIndex + 1 < modelName.Length)
        {
            modelName = modelName[(categoryEndIndex + 1)..].Trim();
        }

        if (string.IsNullOrWhiteSpace(modelName))
        {
            modelName = "AAAA";
        }

        return $"型号{modelName}";
    }
}
