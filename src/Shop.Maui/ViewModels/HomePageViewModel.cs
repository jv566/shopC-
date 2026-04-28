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

    private bool _isInitialized;
    private int _currentBannerIndex;

    public ObservableCollection<ProductCategoryOption> Categories { get; } = [];

    public ObservableCollection<HomeCarouselBanner> Banners { get; private set; } = [];

    public int CurrentBannerIndex
    {
        get => _currentBannerIndex;
        private set
        {
            if (SetProperty(ref _currentBannerIndex, value))
            {
                OnPropertyChanged(nameof(CurrentBanner));
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

    public ICommand NavigateToProductListCommand { get; }
    public ICommand NavigateToPanoramaCommand { get; }
    public ICommand NavigateTo3DShowcaseCommand { get; }

    public HomePageViewModel(
        IProductCategoryProvider categoryProvider,
        IHomeCarouselProvider carouselProvider,
        INavigationService navigationService)
    {
        _categoryProvider = categoryProvider;
        _carouselProvider = carouselProvider;
        _navigationService = navigationService;

        NavigateToProductListCommand = new Command<ProductCategoryOption>(async category =>
        {
            if (category is not null)
            {
                await _navigationService.GoToProductListAsync(category.Id, category.DisplayName);
            }
        });

        NavigateToPanoramaCommand = new Command(async () => await _navigationService.GoToProductPanoramaAsync());
        NavigateTo3DShowcaseCommand = new Command(async () => await _navigationService.GoToProduct3DShowcaseAsync());
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

        _isInitialized = true;
    }
}
