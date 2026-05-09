namespace Shop.Maui.Services;

public interface IImageCacheService
{
    Task<string> GetCachedImageSourceAsync(
        string imageSource,
        CancellationToken cancellationToken = default);
}
