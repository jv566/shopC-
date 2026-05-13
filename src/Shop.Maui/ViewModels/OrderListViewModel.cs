using System.Collections.ObjectModel;
using Shop.Maui.Models;
using Shop.Maui.Services;

namespace Shop.Maui.ViewModels;

public class OrderListViewModel : ObservableObject
{
    private readonly IUserActionService _userActionService;
    private readonly bool _loadHistory;
    private decimal _totalAmount;

    public ObservableCollection<OrderListItem> Orders { get; } = [];

    public string PageTitle => _loadHistory ? "历史订单" : "我的订单";

    public string EmptyText => _loadHistory ? "暂无历史订单" : "暂无进行中的订单";

    public bool HasOrders => Orders.Count > 0;

    public bool IsEmpty => !HasOrders;

    public string OrderCountText => $"共 {Orders.Count} 笔订单";

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

    public OrderListViewModel(IUserActionService userActionService, bool loadHistory)
    {
        _userActionService = userActionService;
        _loadHistory = loadHistory;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var orders = _loadHistory
            ? await _userActionService.GetHistoryOrdersAsync(cancellationToken)
            : await _userActionService.GetMyOrdersAsync(cancellationToken);

        ReplaceCollection(Orders, orders);
        TotalAmount = Orders.Sum(order => order.TotalAmount);
        OnPropertyChanged(nameof(HasOrders));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(OrderCountText));
    }
}
