namespace Shop.Maui.Services;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string phone, string password, CancellationToken cancellationToken = default);

    Task<AuthResult> SendRegisterCodeAsync(string phone, CancellationToken cancellationToken = default);

    Task<AuthResult> RegisterAsync(string phone, string password, string confirmPassword, string checkCode, CancellationToken cancellationToken = default);

    Task<AuthResult> SendResetPasswordCodeAsync(string phone, CancellationToken cancellationToken = default);

    Task<AuthResult> ResetPasswordAsync(string phone, string password, string confirmPassword, string checkCode, CancellationToken cancellationToken = default);
}

public sealed record AuthResult(bool Succeeded, string Message, string? ItsId = null);
