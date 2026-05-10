namespace Shop.Maui.Services;

public interface IImageCacheService
{
    string GetBestImageSource(string imageSource);

    Task<string> GetCachedImageSourceAsync(
        string imageSource,
        CancellationToken cancellationToken = default);
}
