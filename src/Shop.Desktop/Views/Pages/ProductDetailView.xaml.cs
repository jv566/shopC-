using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Shop.Desktop.Models;
using Shop.Desktop.Services;
using Shop.Desktop.ViewModels;

namespace Shop.Desktop.Views.Pages;

public partial class ProductDetailView : UserControl
{
    private const int ImageSlideDurationMs = 360;

    private bool _isLoadedOnce;
    private bool _isSlideAnimating;

    private readonly ProductDetailViewModel _viewModel;

    public ProductDetailView()
        : this(new ProductListItem("unknown", "unknown", "未指定型号", 0m, null))
    {
    }

    public ProductDetailView(ProductListItem product)
    {
        _viewModel = new ProductDetailViewModel(product, DesktopServices.ProductColorVariantProvider);
        DataContext = _viewModel;

        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_isLoadedOnce)
        {
            return;
        }

        _isLoadedOnce = true;
        await _viewModel.InitializeAsync();
        RenderCurrentImage();
    }

    private void OnPrevImageClicked(object sender, RoutedEventArgs e)
    {
        SwitchRelative(-1);
    }

    private void OnNextImageClicked(object sender, RoutedEventArgs e)
    {
        SwitchRelative(1);
    }

    private void OnColorOptionClicked(object sender, RoutedEventArgs e)
    {
        if (_isSlideAnimating || sender is not Button button || !TryGetTargetIndex(button.Tag, out var targetIndex))
        {
            return;
        }

        if (!_viewModel.IsValidColorIndex(targetIndex) || targetIndex == _viewModel.CurrentColorIndex)
        {
            return;
        }

        var direction = _viewModel.ResolveDirectionForJump(targetIndex);
        AnimateToColorOption(targetIndex, direction);
    }

    private void SwitchRelative(int step)
    {
        if (_isSlideAnimating || !_viewModel.TryGetRelativeTargetIndex(step, out var targetIndex, out var direction))
        {
            return;
        }

        AnimateToColorOption(targetIndex, direction);
    }

    private void AnimateToColorOption(int targetIndex, int direction)
    {
        if (!_viewModel.IsValidColorIndex(targetIndex) || targetIndex == _viewModel.CurrentColorIndex)
        {
            return;
        }

        var target = _viewModel.GetColorOption(targetIndex);
        if (target is null)
        {
            return;
        }

        SetNextSlideContent(target);

        var width = ProductImageViewport.ActualWidth;
        if (width < 2)
        {
            _viewModel.SetCurrentColorIndex(targetIndex);
            RenderCurrentImage();
            return;
        }

        _isSlideAnimating = true;

        NextProductSlideLayer.Visibility = Visibility.Visible;

        CurrentProductSlideTransform.X = 0;
        NextProductSlideTransform.X = direction > 0 ? width : -width;

        var duration = TimeSpan.FromMilliseconds(ImageSlideDurationMs);
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

        CurrentProductSlideTransform.BeginAnimation(TranslateTransform.XProperty, currentAnimation);
        NextProductSlideTransform.BeginAnimation(TranslateTransform.XProperty, nextAnimation);
    }

    private void CompleteSlideSwitch(int targetIndex)
    {
        CurrentProductSlideTransform.BeginAnimation(TranslateTransform.XProperty, null);
        NextProductSlideTransform.BeginAnimation(TranslateTransform.XProperty, null);

        CurrentProductSlideTransform.X = 0;
        NextProductSlideTransform.X = 0;
        NextProductSlideLayer.Visibility = Visibility.Collapsed;

        _viewModel.SetCurrentColorIndex(targetIndex);
        _isSlideAnimating = false;

        RenderCurrentImage();
    }

    private void RenderCurrentImage()
    {
        var current = _viewModel.GetCurrentColorOption();
        if (current is null)
        {
            CurrentColorNameText.Text = "颜色：默认色";
            ProductImageHintText.Text = "图片接口: ColorImageUrl（待后端返回）";
            ColorIndexText.Text = "0/0";
            PrevImageButton.IsEnabled = false;
            NextImageButton.IsEnabled = false;
            return;
        }

        CurrentColorNameText.Text = $"颜色：{current.ColorName}";
        ProductImageHintText.Text = ProductDetailViewModel.BuildImageHintText(current.ImageUrl);
        ColorIndexText.Text = _viewModel.ColorIndexText;

        PrevImageButton.IsEnabled = _viewModel.CanSwitchColor;
        NextImageButton.IsEnabled = _viewModel.CanSwitchColor;
    }

    private void SetNextSlideContent(ProductColorImageOption option)
    {
        NextColorNameText.Text = $"颜色：{option.ColorName}";
        NextProductImageHintText.Text = ProductDetailViewModel.BuildImageHintText(option.ImageUrl);
    }

    private static bool TryGetTargetIndex(object? tag, out int index)
    {
        if (tag is int intTag)
        {
            index = intTag;
            return true;
        }

        if (tag is string textTag && int.TryParse(textTag, out var parsed))
        {
            index = parsed;
            return true;
        }

        index = -1;
        return false;
    }

    private void OnBackToListClicked(object sender, RoutedEventArgs e)
    {
        DesktopServices.Navigation.NavigateToProductList(ResolveCategoryOption(_viewModel.Product.CategoryId));
    }

    private static ProductCategoryOption ResolveCategoryOption(string? categoryKey)
    {
        if (string.IsNullOrWhiteSpace(categoryKey))
        {
            return new ProductCategoryOption("unknown", "未指定");
        }

        var matchedGroup = ProductCategoryCatalog.ResolvePrimaryGroup(categoryKey);
        if (matchedGroup is null)
        {
            return new ProductCategoryOption(categoryKey, categoryKey);
        }

        var matchedSecondary = matchedGroup.SecondaryCategories.FirstOrDefault(s =>
            string.Equals(s.Id, categoryKey, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(s.DisplayName, categoryKey, StringComparison.OrdinalIgnoreCase));

        if (matchedSecondary is not null)
        {
            return matchedSecondary;
        }

        if (string.Equals(matchedGroup.PrimaryCategory.Id, categoryKey, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(matchedGroup.PrimaryCategory.DisplayName, categoryKey, StringComparison.OrdinalIgnoreCase))
        {
            return matchedGroup.PrimaryCategory;
        }

        return new ProductCategoryOption(categoryKey, categoryKey);
    }
}
