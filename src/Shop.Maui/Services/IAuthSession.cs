namespace Shop.Maui.Services;

public interface IAuthSession
{
    string? ItsId { get; }

    string? Phone { get; }

    bool IsLoggedIn { get; }

    void SetLoginSession(string phone, string itsId);

    void Clear();
}
