using System.Collections.ObjectModel;
using Shop.Maui.Models;
using Shop.Maui.Services;

namespace Shop.Maui.ViewModels;

public sealed class CartViewModel : ObservableObject
{
    private readonly IUserActionService _userActionService;
    private decimal _totalAmount;

    public ObservableCollection<CartLineItem> Items { get; } = [];

    public bool HasItems => Items.Count > 0;

    public bool IsEmpty => !HasItems;

    public string ItemCountText => $"共 {Items.Sum(item => item.Quantity)} 件商品";

    public string TotalAmountText => $"￥{TotalAmount:F2}";

    public decimal TotalAmount
    {
        get => _totalAmount;
        private set
        {
            if (SetProperty(ref _totalAmount, value))
            {
                OnPropertyChanged(nameof(TotalAmountText));
            }
        }
    }

    public CartViewModel(IUserActionService userActionService)
    {
        _userActionService = userActionService;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var items = await _userActionService.GetCartItemsAsync(cancellationToken);
        ReplaceCollection(Items, items);
        TotalAmount = Items.Sum(item => item.Subtotal);
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(ItemCountText));
    }
}
