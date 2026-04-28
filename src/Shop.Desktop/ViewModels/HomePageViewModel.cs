using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Shop.Desktop.Models;
using Shop.Desktop.Services;

namespace Shop.Desktop.ViewModels
{
    public sealed class HomePageViewModel(IProductCategoryProvider categoryProvider, IHomeCarouselProvider carouselProvider) : ObservableObject
    {
        private readonly IProductCategoryProvider _categoryProvider = categoryProvider;
        private readonly IHomeCarouselProvider _carouselProvider = carouselProvider;

        private bool _isInitialized;
        private int _currentBannerIndex;

        public ObservableCollection<ProductCategoryOption> Categories { get; } = [];

        public ObservableCollection<HomeCarouselBanner> Banners { get; private set; } =
        [
            new("banner1", "第一张轮播图",
                "pack://application:,,,/Shop.Desktop;component/Assets/Images/lunbotu.png",
                "描述1"),
            new("banner1", "第一张轮播图",
                "pack://application:,,,/Shop.Desktop;component/Assets/Images/lunbotu.png",
                "描述1"),
            new("banner1", "第一张轮播图",
                "pack://application:,,,/Shop.Desktop;component/Assets/Images/lunbotu.png",
                "描述1"),
        ];


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

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (_isInitialized) return;

            var categories = await _categoryProvider.GetCategoriesAsync(cancellationToken);
            ReplaceCollection(Categories, categories);

            Banners =
            [
                new("banner1", "第一张轮播图",
            "pack://application:,,,/Shop.Desktop;component/Assets/Images/lunbotu.png",
            "描述1"),
        new("banner2", "第二张轮播图",
            "pack://application:,,,/Shop.Desktop;component/Assets/Images/lunbotu.png",
            "描述2"),
        new("banner3", "第三张轮播图",
            "pack://application:,,,/Shop.Desktop;component/Assets/Images/lunbotu.png",
            "描述3"),
    ];

            if (Banners.Count == 0)
            {
                Banners =
                [
                    new HomeCarouselBanner("placeholder", "轮播图片占位", null, "暂无轮播数据")
                ];
            }

            CurrentBannerIndex = 0;
            CurrentBanner = Banners[0];   // ✅ 关键：初始化时设置当前 Banner

            _isInitialized = true;

            OnPropertyChanged(nameof(Banners));
            OnPropertyChanged(nameof(CurrentBanner));
            OnPropertyChanged(nameof(BannerIndexText));
            OnPropertyChanged(nameof(CanSwitchBanner));
        }


        public bool TryGetSwitchTarget(int step, out int targetIndex, out int direction)
        {
            targetIndex = CurrentBannerIndex;
            direction = 1;

            if (!CanSwitchBanner) return false;

            direction = step >= 0 ? 1 : -1;
            targetIndex = (CurrentBannerIndex + step + Banners.Count) % Banners.Count;
            return true;
        }

        public HomeCarouselBanner? GetBanner(int index)
        {
            if (index < 0 || index >= Banners.Count) return null;
            return Banners[index];
        }

        public void SetCurrentBannerIndex(int targetIndex)
        {
            if (targetIndex < 0 || targetIndex >= Banners.Count) return;
            CurrentBannerIndex = targetIndex;
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
}
