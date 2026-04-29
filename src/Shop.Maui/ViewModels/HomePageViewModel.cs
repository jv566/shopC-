using System.Collections.ObjectModel;
using System.Windows.Input;
using Shop.Maui.Models;
using Shop.Maui.Services;

namespace Shop.Maui.ViewModels;

public sealed class HomePageViewModel : ObservableObject
{
    private readonly IProductCategoryProvider _categoryProvider;
    private readonly IHomeCarouselProvider _carouselProvider;
    private readonly INavigationService _navigationService;
    private readonly IUserActionService _userActionService;

    private bool _isInitialized;
    private int _currentBannerIndex;
    private Timer? _bannerTimer;

    public ObservableCollection<ProductCategoryOption> Categories { get; } = [];

    public ObservableCollection<HomeCarouselBanner> Banners { get; private set; } = [];

    public int CurrentBannerIndex
    {
        get => _currentBannerIndex;
        private set
        {
            if (SetProperty(ref _currentBannerIndex, value))
            {
                CurrentBanner = Banners.Count > 0 ? Banners[value] : null;
                OnPropertyChanged(nameof(BannerIndexText));
                OnPropertyChanged(nameof(CanSwitchBanner));
            }
        }
    }

    private HomeCarouselBanner? _currentBanner;
    public HomeCarouselBanner? CurrentBanner
    {
        get => _currentBanner;
        private set
        {
            if (_currentBanner != value)
            {
                _currentBanner = value;
                OnPropertyChanged(nameof(CurrentBanner));
            }
        }
    }

    public string BannerIndexText =>
        Banners.Count == 0 ? "0/0" : $"{CurrentBannerIndex + 1}/{Banners.Count}";

    public bool CanSwitchBanner => Banners.Count > 1;

    public ICommand NavigateToCategoryByIdCommand { get; }
    public ICommand NavigateToPanoramaCommand { get; }
    public ICommand NavigateTo3DShowcaseCommand { get; }
    public ICommand OpenCartCommand { get; }
    public ICommand OpenMyOrdersCommand { get; }
    public ICommand OpenHistoryOrdersCommand { get; }
    public ICommand SyncQrCommand { get; }
    public ICommand NextBannerCommand { get; }
    public ICommand PrevBannerCommand { get; }

    public HomePageViewModel(
        IProductCategoryProvider categoryProvider,
        IHomeCarouselProvider carouselProvider,
        INavigationService navigationService,
        IUserActionService userActionService)
    {
        _categoryProvider = categoryProvider;
        _carouselProvider = carouselProvider;
        _navigationService = navigationService;
        _userActionService = userActionService;

        NavigateToCategoryByIdCommand = new Command<string>(async categoryId =>
        {
            if (string.IsNullOrWhiteSpace(categoryId)) return;
            var category = Categories.FirstOrDefault(c => string.Equals(c.Id, categoryId, StringComparison.OrdinalIgnoreCase));
            if (category is not null)
            {
                await _navigationService.GoToCategoryWallAsync(category.Id, category.DisplayName);
            }
            else
            {
                await _navigationService.GoToCategoryWallAsync(categoryId, categoryId);
            }
        });

        NavigateToPanoramaCommand = new Command(async () => await _navigationService.GoToProductPanoramaAsync());
        NavigateTo3DShowcaseCommand = new Command(async () => await _navigationService.GoToProduct3DShowcaseAsync());
        OpenCartCommand = new Command(async () => await ShowActionResultAsync(_userActionService.GetCartSummaryAsync()));
        OpenMyOrdersCommand = new Command(async () => await ShowActionResultAsync(_userActionService.GetMyOrdersSummaryAsync()));
        OpenHistoryOrdersCommand = new Command(async () => await ShowActionResultAsync(_userActionService.GetHistoryOrdersSummaryAsync()));
        SyncQrCommand = new Command(async () => await ShowActionResultAsync(_userActionService.SyncQrAsync()));

        NextBannerCommand = new Command(() =>
        {
            if (Banners.Count > 0)
            {
                CurrentBannerIndex = (CurrentBannerIndex + 1) % Banners.Count;
            }
        });

        PrevBannerCommand = new Command(() =>
        {
            if (Banners.Count > 0)
            {
                CurrentBannerIndex = (CurrentBannerIndex - 1 + Banners.Count) % Banners.Count;
            }
        });
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized) return;

        var categories = await _categoryProvider.GetCategoriesAsync(cancellationToken);
        ReplaceCollection(Categories, categories);

        var banners = await _carouselProvider.GetBannersAsync(cancellationToken);
        ReplaceCollection(Banners, banners);

        if (Banners.Count == 0)
        {
            Banners.Add(new HomeCarouselBanner("placeholder", "轮播图片占位", null, "暂无轮播数据"));
        }

        CurrentBannerIndex = 0;
        CurrentBanner = Banners.Count > 0 ? Banners[0] : null;

        StartBannerAutoPlay();

        _isInitialized = true;
    }

    private void StartBannerAutoPlay()
    {
        _bannerTimer?.Dispose();
        if (Banners.Count > 1)
        {
            _bannerTimer = new Timer(_ =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (Banners.Count > 0)
                    {
                        CurrentBannerIndex = (CurrentBannerIndex + 1) % Banners.Count;
                    }
                });
            }, null, TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(4));
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

    private static async Task ShowActionResultAsync(Task<UserActionResult> actionTask)
    {
        var result = await actionTask;
        if (Shell.Current is not null)
        {
            await Shell.Current.DisplayAlert(result.Title, result.Message, "确定");
        }
    }
}
