namespace Shop.Maui.Services;

public sealed class AuthSession : IAuthSession
{
    public string? ItsId { get; private set; }

    public string? UnitId { get; private set; }

    public string? Phone { get; private set; }

    public bool IsLoggedIn => !string.IsNullOrWhiteSpace(ItsId);

    public void SetLoginSession(string phone, string itsId, string? unitId)
    {
        Phone = phone;
        ItsId = itsId;
        UnitId = unitId;
        Console.WriteLine($"SetLoginSession: {phone}, {itsId}, {unitId}");
    }

    public void Clear()
    {
        Phone = null;
        ItsId = null;
        UnitId = null;
    }
}
