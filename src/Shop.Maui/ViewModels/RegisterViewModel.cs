using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Shop.Maui.Services;
using Shop.Maui.Views;

namespace Shop.Maui.ViewModels;

public sealed class RegisterViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly IServiceProvider _serviceProvider;
    private string _account = string.Empty;
    private string _password = string.Empty;
    private string _confirmPassword = string.Empty;

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

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set => SetProperty(ref _confirmPassword, value);
    }

    public ICommand RegisterCommand { get; }

    public ICommand GoToLoginCommand { get; }

    public RegisterViewModel(IAuthService authService, IServiceProvider serviceProvider)
    {
        _authService = authService;
        _serviceProvider = serviceProvider;
        RegisterCommand = new Command(async () => await RegisterAsync());
        GoToLoginCommand = new Command(GoToLogin);
    }

    private async Task RegisterAsync()
    {
        if (string.IsNullOrWhiteSpace(Account) ||
            string.IsNullOrWhiteSpace(Password) ||
            !string.Equals(Password, ConfirmPassword, StringComparison.Ordinal))
        {
            if (Microsoft.Maui.Controls.Application.Current?.MainPage is not null)
            {
                await Microsoft.Maui.Controls.Application.Current.MainPage.DisplayAlert("注册失败", "请填写账号、密码，并确认两次密码一致。", "确定");
            }

            return;
        }

        await _authService.RegisterAsync(Account, Password);
        if (Microsoft.Maui.Controls.Application.Current?.MainPage is not null)
        {
            await Microsoft.Maui.Controls.Application.Current.MainPage.DisplayAlert("注册成功", "请使用新账号登录。", "确定");
        }

        GoToLogin();
    }

    private void GoToLogin()
    {
        if (Microsoft.Maui.Controls.Application.Current is not null)
        {
            Microsoft.Maui.Controls.Application.Current.MainPage = _serviceProvider.GetRequiredService<LoginPage>();
        }
    }
}
