using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Shop.Desktop.Configuration;
using Shop.Desktop.Models;
using Shop.Desktop.Services;
using Shop.Desktop.ViewModels;

namespace Shop.Desktop.Views.Pages
{
    public partial class HomePageView : UserControl
    {
        private bool _isLoadedOnce;
        private bool _isSlideAnimating;
        private bool _isCarouselHovered;

        private readonly DispatcherTimer _autoCarouselTimer;
        private readonly HomePageViewModel _viewModel;

        public HomePageView()
        {
            _viewModel = new HomePageViewModel(
                DesktopServices.ProductCategoryProvider,
                DesktopServices.HomeCarouselProvider);

            DataContext = _viewModel;

            InitializeComponent();

            _autoCarouselTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(PanoramaWebViewSettings.HomeCarouselIntervalSeconds)
            };

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            _autoCarouselTimer.Tick += OnAutoCarouselTick;
        }

        private void SettingOnClick(object sender, MouseEventArgs e)
        {
            Window.GetWindow(this)?.Close();
        }

        private void OnEnterProduct3DClick(object sender, RoutedEventArgs e)
        {
            DesktopServices.Navigation.NavigateToProduct3DShowcase();
        }

        private void OnEnterPanoramaReplacementClick(object sender, RoutedEventArgs e)
        {
            DesktopServices.Navigation.NavigateToProductPanoramaReplacement();
        }

        private void OnCategoryClick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: ProductCategoryOption category })
            {
                return;
            }

            DesktopServices.Navigation.NavigateToProductList(category);
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_isLoadedOnce)
            {
                UpdateAutoCarouselState();
                return;
            }

            _isLoadedOnce = true;

            await _viewModel.InitializeAsync();
            RenderCurrentBanner();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _autoCarouselTimer.Stop();
            _isCarouselHovered = false;
        }

        private void OnAutoCarouselTick(object? sender, EventArgs e)
        {
            if (_isCarouselHovered || _isSlideAnimating || !_viewModel.CanSwitchBanner)
            {
                return;
            }

            TrySwitchBanner(1);
        }

        private void OnCarouselMouseEnter(object sender, MouseEventArgs e)
        {
            _isCarouselHovered = true;
            UpdateAutoCarouselState();
        }

        private void OnCarouselMouseLeave(object sender, MouseEventArgs e)
        {
            _isCarouselHovered = false;
            UpdateAutoCarouselState();
        }

        private void TrySwitchBanner(int step)
        {
            if (_isSlideAnimating || !_viewModel.TryGetSwitchTarget(step, out var targetIndex, out var direction))
            {
                return;
            }

            AnimateToBanner(targetIndex, direction);
        }

        private void AnimateToBanner(int targetIndex, int direction)
        {
            var target = _viewModel.GetBanner(targetIndex);
            if (target is null) return;

            SetNextSlideContent(target);

            var width = CarouselSlideViewport.ActualWidth;
            if (width < 2)
            {
                _viewModel.SetCurrentBannerIndex(targetIndex);
                RenderCurrentBanner();
                return;
            }

            _isSlideAnimating = true;

            NextSlideLayer.Visibility = Visibility.Visible;
            CurrentSlideTransform.X = 0;
            NextSlideTransform.X = direction > 0 ? width : -width;

            var duration = TimeSpan.FromMilliseconds(PanoramaWebViewSettings.HomeCarouselSlideDurationMs);
            var easing = new CubicEase { EasingMode = EasingMode.EaseOut };

            var currentAnimation = new DoubleAnimation
            {
                To = direction > 0 ? -width : width,
                Duration = duration,
                EasingFunction = easing
            };

            var nextAnimation = new DoubleAnimation
            {
                To = 0,
                Duration = duration,
                EasingFunction = easing
            };

            nextAnimation.Completed += (_, _) => CompleteSlideSwitch(targetIndex);

            CurrentSlideTransform.BeginAnimation(TranslateTransform.XProperty, currentAnimation);
            NextSlideTransform.BeginAnimation(TranslateTransform.XProperty, nextAnimation);
        }

        private void CompleteSlideSwitch(int targetIndex)
        {
            CurrentSlideTransform.BeginAnimation(TranslateTransform.XProperty, null);
            NextSlideTransform.BeginAnimation(TranslateTransform.XProperty, null);

            CurrentSlideTransform.X = 0;
            NextSlideTransform.X = 0;
            NextSlideLayer.Visibility = Visibility.Collapsed;

            _viewModel.SetCurrentBannerIndex(targetIndex);
            RenderCurrentBanner();

            _isSlideAnimating = false;
            UpdateAutoCarouselState();
        }

        private void RenderCurrentBanner()
        {
            var current = _viewModel.CurrentBanner;
            if (current is null)
            {
                RenderCarouselDotIndicator();
                UpdateAutoCarouselState();
                return;
            }

            SetCurrentSlideContent(current);
            RenderCarouselDotIndicator();
            UpdateAutoCarouselState();
        }

        private void SetCurrentSlideContent(HomeCarouselBanner banner)
        {
            if (banner?.ImageSource == null)
            {
                ((ImageBrush)CurrentSlideRect.Fill).ImageSource = null;
                Debug.WriteLine("ImageSource is null.");
                return;
            }

            try
            {
                ((ImageBrush)CurrentSlideRect.Fill).ImageSource = banner.ImageSource;
                Debug.WriteLine("Image loaded successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Image set failed: {ex.Message}");
                ((ImageBrush)CurrentSlideRect.Fill).ImageSource = null;
            }
        }

        private void SetNextSlideContent(HomeCarouselBanner banner)
        {
            ((ImageBrush)NextSlideRect.Fill).ImageSource = banner?.ImageSource;
        }

        private void UpdateAutoCarouselState()
        {
            var shouldRun =
                PanoramaWebViewSettings.HomeCarouselAutoPlayEnabled &&
                _viewModel.CanSwitchBanner &&
                !_isCarouselHovered &&
                !_isSlideAnimating &&
                IsLoaded;

            if (shouldRun)
            {
                if (!_autoCarouselTimer.IsEnabled)
                {
                    _autoCarouselTimer.Start();
                }
            }
            else
            {
                _autoCarouselTimer.Stop();
            }
        }

        private void RenderCarouselDotIndicator()
        {
            CarouselIndicatorPanel.Children.Clear();

            var total = _viewModel.Banners.Count;
            if (total <= 0) return;

            foreach (var index in Enumerable.Range(0, total))
            {
                CarouselIndicatorPanel.Children.Add(new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Margin = new Thickness(5, 0, 5, 0),
                    Fill = index == _viewModel.CurrentBannerIndex
                        ? new SolidColorBrush(Color.FromRgb(0xF4, 0xBF, 0x56)) // 黄色
                        : new SolidColorBrush(Color.FromArgb(128, 200, 200, 200)), // 灰色
                });
            }
        }
    }
}
