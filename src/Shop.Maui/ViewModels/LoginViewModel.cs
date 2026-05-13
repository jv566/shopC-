using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Shop.Maui.Services;
using Shop.Maui.Views;

namespace Shop.Maui.ViewModels;

public sealed class LoginViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly IServiceProvider _serviceProvider;
    private string _phone = string.Empty;
    private string _password = string.Empty;
    private bool _rememberAccount = true;
    private bool _isBusy;

    public string Phone
    {
        get => _phone;
        set => SetProperty(ref _phone, value);
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

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public ICommand LoginCommand { get; }

    public ICommand GoToRegisterCommand { get; }

    public ICommand GoToResetPasswordCommand { get; }

    public LoginViewModel(IAuthService authService, IServiceProvider serviceProvider)
    {
        _authService = authService;
        _serviceProvider = serviceProvider;

        LoginCommand = new Command(async () => await LoginAsync());
        GoToRegisterCommand = new Command(GoToRegister);
        GoToResetPasswordCommand = new Command(GoToResetPassword);
    }

    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Phone) || string.IsNullOrWhiteSpace(Password))
        {
            await ShowAlertAsync("登录失败", "请输入手机号和密码。");
            return;
        }

        IsBusy = true;
        var result = await _authService.LoginAsync(Phone, Password);
        IsBusy = false;

        if (!result.Succeeded)
        {
            await ShowAlertAsync("登录失败", result.Message);
            return;
        }

        if (string.IsNullOrWhiteSpace(result.ItsId))
        {
            await ShowAlertAsync("登录失败", "登录接口未返回有效身份信息，请确认账号密码是否正确。");
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

    private void GoToResetPassword()
    {
        if (Microsoft.Maui.Controls.Application.Current is not null)
        {
            Microsoft.Maui.Controls.Application.Current.MainPage = _serviceProvider.GetRequiredService<ResetPasswordPage>();
        }
    }

    private static async Task ShowAlertAsync(string title, string message)
    {
        if (Microsoft.Maui.Controls.Application.Current?.MainPage is not null)
        {
            await Microsoft.Maui.Controls.Application.Current.MainPage.DisplayAlert(title, message, "确定");
        }
    }
}
