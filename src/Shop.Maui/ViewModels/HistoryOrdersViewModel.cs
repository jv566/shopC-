using Shop.Maui.Services;

namespace Shop.Maui.ViewModels;

public sealed class HistoryOrdersViewModel(IUserActionService userActionService)
    : OrderListViewModel(userActionService, loadHistory: true);
