namespace Shop.Maui.Services;

public sealed class AuthSession : IAuthSession
{
    public string? ItsId { get; private set; }

    public string? Phone { get; private set; }

    public bool IsLoggedIn => !string.IsNullOrWhiteSpace(ItsId);

    public void SetLoginSession(string phone, string itsId)
    {
        Phone = phone;
        ItsId = itsId;
    }

    public void Clear()
    {
        Phone = null;
        ItsId = null;
    }
}
