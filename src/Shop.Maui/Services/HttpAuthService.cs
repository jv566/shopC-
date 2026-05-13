using System.Net.Http.Json;
using System.Text.Json;

namespace Shop.Maui.Services;

public sealed class HttpAuthService : IAuthService
{
    private const string AccountType = "子女端账号";
    private const string LoginUrl = "https://www.ruanzi.net/jy/go/phone.aspx?ituid=121&mbid=10300";
    private const string SendRegisterCodeUrl = "https://www.ruanzi.net/jy/go/phone.aspx?ituid=121&mbid=10326";
    private const string RegisterUrl = "https://www.ruanzi.net/jy/go/phone.aspx?ituid=121&mbid=10311";
    private const string SendResetPasswordCodeUrl = "https://www.ruanzi.net/jy/go/phone.aspx?ituid=121&mbid=1236";
    private const string ResetPasswordUrl = "https://www.ruanzi.net/jy/go/phone.aspx?ituid=121&mbid=11608";

    private readonly IAuthSession _authSession;
    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(12)
    };

    public HttpAuthService(IAuthSession authSession)
    {
        _authSession = authSession;
    }

    public async Task<AuthResult> LoginAsync(string phone, string password, CancellationToken cancellationToken = default)
    {
        var normalizedPhone = Normalize(phone);
        var result = await PostAsync(
            LoginUrl,
            new
            {
                name = normalizedPhone,
                pwd = password
            },
            "登录成功",
            cancellationToken);

        if (result.Succeeded && !string.IsNullOrWhiteSpace(result.ItsId))
        {
            // 登录接口返回的 itsid 是后续用户态接口的关键身份字段。
            // 下单、我的订单、历史订单等接口对接后，应从 IAuthSession.ItsId 读取并随请求上传。
            _authSession.SetLoginSession(normalizedPhone, result.ItsId);
        }

        return result;
    }

    public Task<AuthResult> SendRegisterCodeAsync(string phone, CancellationToken cancellationToken = default)
    {
        return PostAsync(
            SendRegisterCodeUrl,
            new
            {
                name = Normalize(phone),
                time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            },
            "验证码已发送",
            cancellationToken);
    }

    public Task<AuthResult> RegisterAsync(
        string phone,
        string password,
        string confirmPassword,
        string checkCode,
        CancellationToken cancellationToken = default)
    {
        return PostAsync(
            RegisterUrl,
            new
            {
                edMobile = Normalize(phone),
                edPWD = password,
                edPWD2 = confirmPassword,
                edCheckCode = Normalize(checkCode),
                accountType = AccountType
            },
            "注册成功",
            cancellationToken);
    }

    public Task<AuthResult> SendResetPasswordCodeAsync(string phone, CancellationToken cancellationToken = default)
    {
        return PostAsync(
            SendResetPasswordCodeUrl,
            new
            {
                name = Normalize(phone),
                time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            },
            "验证码已发送",
            cancellationToken);
    }

    public Task<AuthResult> ResetPasswordAsync(
        string phone,
        string password,
        string confirmPassword,
        string checkCode,
        CancellationToken cancellationToken = default)
    {
        return PostAsync(
            ResetPasswordUrl,
            new
            {
                edMobile = Normalize(phone),
                edPWD = password,
                edPWD2 = confirmPassword,
                edCheckCode = Normalize(checkCode),
                accountType = AccountType
            },
            "密码修改成功",
            cancellationToken);
    }

    private async Task<AuthResult> PostAsync(
        string url,
        object payload,
        string successMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new AuthResult(false, ExtractMessage(body, $"请求失败：{response.StatusCode}"), ExtractItsId(body));
            }

            var itsId = ExtractItsId(body);
            return LooksSuccessful(body)
                ? new AuthResult(true, ExtractMessage(body, successMessage), itsId)
                : new AuthResult(false, ExtractMessage(body, "操作失败，请检查输入后重试。"), itsId);
        }
        catch (Exception ex)
        {
            return new AuthResult(false, $"网络请求失败：{ex.Message}", null);
        }
    }

    private static bool LooksSuccessful(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return true;
        }

        var text = body.Trim();
        if (text.Contains("失败", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("错误", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("不存在", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("error", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("\"success\":false", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (text.Contains("成功", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("\"success\":true", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        try
        {
            using var document = JsonDocument.Parse(text);
            var root = document.RootElement;
            foreach (var name in new[] { "code", "status", "state", "result" })
            {
                if (!TryFindProperty(root, name, out var property))
                {
                    continue;
                }

                if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var number))
                {
                    return number is 0 or 1 or 200;
                }

                if (property.ValueKind == JsonValueKind.String)
                {
                    var value = property.GetString();
                    return value is "0" or "1" or "200" or "success" or "ok";
                }
            }
        }
        catch
        {
        }

        return true;
    }

    private static string ExtractMessage(string body, string fallback)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return fallback;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            foreach (var name in new[] { "msg", "message", "desc", "error", "info" })
            {
                if (TryFindProperty(root, name, out var property) && property.ValueKind == JsonValueKind.String)
                {
                    var message = property.GetString();
                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        return message;
                    }
                }
            }
        }
        catch
        {
        }

        return body.Length > 80 ? fallback : body;
    }

    private static string? ExtractItsId(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            return TryFindProperty(document.RootElement, "itsid", out var property)
                ? property.ToString()
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static bool TryFindProperty(JsonElement element, string propertyName, out JsonElement property)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var item in element.EnumerateObject())
            {
                if (string.Equals(item.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    property = item.Value;
                    return true;
                }

                if (TryFindProperty(item.Value, propertyName, out property))
                {
                    return true;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                if (TryFindProperty(item, propertyName, out property))
                {
                    return true;
                }
            }
        }

        property = default;
        return false;
    }

    private static string Normalize(string? value)
    {
        return (value ?? string.Empty).Trim();
    }
}
