using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Shop.Maui.Services;
using Shop.Maui.Views;

namespace Shop.Maui.ViewModels;

public sealed class LoginViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly IServiceProvider _serviceProvider;
    private string _account;
    private string _password;
    private bool _rememberAccount = true;

    public string Account
    {
        get => _account;
        set => SetProperty(ref _account, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public bool RememberAccount
    {
        get => _rememberAccount;
        set => SetProperty(ref _rememberAccount, value);
    }

    public ICommand LoginCommand { get; }

    public ICommand GoToRegisterCommand { get; }

    public LoginViewModel(IAuthService authService, IServiceProvider serviceProvider)
    {
        _authService = authService;
        _serviceProvider = serviceProvider;
        _account = authService.DefaultUserName;
        _password = authService.DefaultPassword;

        LoginCommand = new Command(async () => await LoginAsync());
        GoToRegisterCommand = new Command(GoToRegister);
    }

    private async Task LoginAsync()
    {
        var success = await _authService.LoginAsync(Account, Password);
        if (!success)
        {
            if (Microsoft.Maui.Controls.Application.Current?.MainPage is not null)
            {
                await Microsoft.Maui.Controls.Application.Current.MainPage.DisplayAlert("登录失败", "账号或密码不正确。", "确定");
            }

            return;
        }

        if (Microsoft.Maui.Controls.Application.Current is not null)
        {
            Microsoft.Maui.Controls.Application.Current.MainPage = _serviceProvider.GetRequiredService<AppShell>();
        }
    }

    private void GoToRegister()
    {
        if (Microsoft.Maui.Controls.Application.Current is not null)
        {
            Microsoft.Maui.Controls.Application.Current.MainPage = _serviceProvider.GetRequiredService<RegisterPage>();
        }
    }
}
