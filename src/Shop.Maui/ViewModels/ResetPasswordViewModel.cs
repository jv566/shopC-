using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Shop.Maui.Services;
using Shop.Maui.Views;

namespace Shop.Maui.ViewModels;

public sealed class ResetPasswordViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly IServiceProvider _serviceProvider;
    private string _phone = string.Empty;
    private string _password = string.Empty;
    private string _confirmPassword = string.Empty;
    private string _checkCode = string.Empty;
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

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set => SetProperty(ref _confirmPassword, value);
    }

    public string CheckCode
    {
        get => _checkCode;
        set => SetProperty(ref _checkCode, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public ICommand SendCodeCommand { get; }

    public ICommand ResetPasswordCommand { get; }

    public ICommand GoToLoginCommand { get; }

    public ResetPasswordViewModel(IAuthService authService, IServiceProvider serviceProvider)
    {
        _authService = authService;
        _serviceProvider = serviceProvider;
        SendCodeCommand = new Command(async () => await SendCodeAsync());
        ResetPasswordCommand = new Command(async () => await ResetPasswordAsync());
        GoToLoginCommand = new Command(GoToLogin);
    }

    private async Task SendCodeAsync()
    {
        if (string.IsNullOrWhiteSpace(Phone))
        {
            await ShowAlertAsync("发送失败", "请输入手机号。");
            return;
        }

        IsBusy = true;
        var result = await _authService.SendResetPasswordCodeAsync(Phone);
        IsBusy = false;
        await ShowAlertAsync(result.Succeeded ? "验证码" : "发送失败", result.Message);
    }

    private async Task ResetPasswordAsync()
    {
        if (string.IsNullOrWhiteSpace(Phone) ||
            string.IsNullOrWhiteSpace(Password) ||
            string.IsNullOrWhiteSpace(CheckCode) ||
            !string.Equals(Password, ConfirmPassword, StringComparison.Ordinal))
        {
            await ShowAlertAsync("修改失败", "请填写手机号、验证码、新密码，并确认两次密码一致。");
            return;
        }

        IsBusy = true;
        var result = await _authService.ResetPasswordAsync(Phone, Password, ConfirmPassword, CheckCode);
        IsBusy = false;

        await ShowAlertAsync(result.Succeeded ? "修改成功" : "修改失败", result.Message);
        if (result.Succeeded)
        {
            GoToLogin();
        }
    }

    private void GoToLogin()
    {
        if (Microsoft.Maui.Controls.Application.Current is not null)
        {
            Microsoft.Maui.Controls.Application.Current.MainPage = _serviceProvider.GetRequiredService<LoginPage>();
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
