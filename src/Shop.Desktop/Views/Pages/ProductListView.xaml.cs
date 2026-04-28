using System.Windows.Controls;
using Shop.Desktop.Models;
using Shop.Desktop.Services;
using Shop.Desktop.ViewModels;

namespace Shop.Desktop.Views.Pages;

public partial class ProductListView : UserControl
{
    private bool _suppressExpandHandler;

    private readonly ProductListViewModel _viewModel;

    public ProductListView()
        : this(new ProductCategoryOption("unknown", "未指定"))
    {
    }

    public ProductListView(ProductCategoryOption category)
    {
        _viewModel = new ProductListViewModel(
            category,
            DesktopServices.ProductProvider,
            DesktopServices.ProductCategoryTreeProvider);

        DataContext = _viewModel;

        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        Loaded -= OnLoaded;

        await _viewModel.InitializeAsync();

        if (string.IsNullOrWhiteSpace(_viewModel.ActivePrimaryId))
        {
            return;
        }

        await Dispatcher.InvokeAsync(
            () =>
            {
                PrimaryCategoryItems.UpdateLayout();
                ExpandSinglePrimaryCategory(_viewModel.ActivePrimaryId);
            },
            System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private async void OnSecondaryCategoryClicked(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is not Button { Tag: ProductCategoryOption secondaryCategory })
        {
            return;
        }

        await _viewModel.SelectSecondaryCategoryAsync(secondaryCategory);
    }

    private async void OnPrimaryCategoryExpanded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_suppressExpandHandler)
        {
            return;
        }

        if (sender is not Expander { Tag: string primaryId, DataContext: ProductCategoryGroup group })
        {
            return;
        }

        _viewModel.ActivePrimaryId = primaryId;
        ExpandSinglePrimaryCategory(primaryId);
        await _viewModel.SelectPrimaryCategoryAsync(group.PrimaryCategory);
    }

    private void OnPrimaryCategoryLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is not Expander { Tag: string primaryId } expander)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_viewModel.ActivePrimaryId))
        {
            return;
        }

        if (string.Equals(primaryId, _viewModel.ActivePrimaryId, StringComparison.OrdinalIgnoreCase))
        {
            _suppressExpandHandler = true;
            expander.IsExpanded = true;
            _suppressExpandHandler = false;
        }
    }

    private void ExpandSinglePrimaryCategory(string? primaryId)
    {
        if (string.IsNullOrWhiteSpace(primaryId))
        {
            return;
        }

        _suppressExpandHandler = true;

        foreach (var expander in FindVisualChildren<Expander>(PrimaryCategoryItems))
        {
            if (expander.Tag is string id)
            {
                expander.IsExpanded = string.Equals(id, primaryId, StringComparison.OrdinalIgnoreCase);
            }
        }

        _suppressExpandHandler = false;
    }

    private static IEnumerable<T> FindVisualChildren<T>(System.Windows.DependencyObject parent) where T : System.Windows.DependencyObject
    {
        if (parent is null)
        {
            yield break;
        }

        var childCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < childCount; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);

            if (child is T typed)
            {
                yield return typed;
            }

            foreach (var sub in FindVisualChildren<T>(child))
            {
                yield return sub;
            }
        }
    }

    private void OnProductClicked(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is not Button { Tag: ProductListItem product })
        {
            return;
        }

        DesktopServices.Navigation.NavigateToProductDetail(product);
    }

    private void OnBackClicked(object sender, System.Windows.RoutedEventArgs e)
    {
        DesktopServices.Navigation.NavigateToHome();
    }
}
