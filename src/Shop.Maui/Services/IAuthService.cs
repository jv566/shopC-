namespace Shop.Maui.Services;

public interface IAuthService
{
    string DefaultUserName { get; }

    string DefaultPassword { get; }

    Task<bool> LoginAsync(string account, string password, CancellationToken cancellationToken = default);

    Task<bool> RegisterAsync(string account, string password, CancellationToken cancellationToken = default);
}
