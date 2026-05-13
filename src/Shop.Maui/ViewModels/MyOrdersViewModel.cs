using Shop.Maui.Services;

namespace Shop.Maui.ViewModels;

public sealed class MyOrdersViewModel(IUserActionService userActionService)
    : OrderListViewModel(userActionService, loadHistory: false);
