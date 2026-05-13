namespace Shop.Maui.Services;

public sealed class LocalAuthService : IAuthService
{
    private readonly Dictionary<string, string> _accounts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["admin"] = "123456"
    };

    public string DefaultUserName => "admin";

    public string DefaultPassword => "123456";

    public Task<bool> LoginAsync(string account, string password, CancellationToken cancellationToken = default)
    {
        var normalizedAccount = Normalize(account);
        var success = !string.IsNullOrWhiteSpace(normalizedAccount) &&
                      _accounts.TryGetValue(normalizedAccount, out var savedPassword) &&
                      string.Equals(savedPassword, password, StringComparison.Ordinal);

        return Task.FromResult(success);
    }

    public Task<bool> RegisterAsync(string account, string password, CancellationToken cancellationToken = default)
    {
        var normalizedAccount = Normalize(account);
        if (string.IsNullOrWhiteSpace(normalizedAccount) || string.IsNullOrWhiteSpace(password))
        {
            return Task.FromResult(false);
        }

        _accounts[normalizedAccount] = password;
        return Task.FromResult(true);
    }

    private static string Normalize(string? value)
    {
        return (value ?? string.Empty).Trim();
    }
}
