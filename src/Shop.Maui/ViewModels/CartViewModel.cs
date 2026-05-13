using System.Collections.ObjectModel;
using System.Windows.Input;
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

    public ICommand CheckoutCommand { get; }

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
        CheckoutCommand = new Command(async () => await CheckoutAsync(), () => HasItems);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var items = await _userActionService.GetCartItemsAsync(cancellationToken);
        ReplaceCollection(Items, items);
        TotalAmount = Items.Sum(item => item.Subtotal);
        OnCartStateChanged();
    }

    private async Task CheckoutAsync()
    {
        var result = await _userActionService.CheckoutCartAsync();
        if (Shell.Current is not null)
        {
            await Shell.Current.DisplayAlert(result.Title, result.Message, "确定");
        }

        await InitializeAsync();
    }

    private void OnCartStateChanged()
    {
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(ItemCountText));
        (CheckoutCommand as Command)?.ChangeCanExecute();
    }
}
