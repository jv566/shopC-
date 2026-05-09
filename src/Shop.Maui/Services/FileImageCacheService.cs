using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace Shop.Maui.Services;

public sealed class FileImageCacheService : IImageCacheService
{
    private static readonly HashSet<string> KnownExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".gif",
        ".bmp"
    };

    private readonly ConcurrentDictionary<string, Lazy<Task<string>>> _inflight = new();
    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(20)
    };

    public Task<string> GetCachedImageSourceAsync(
        string imageSource,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(imageSource, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return Task.FromResult(imageSource);
        }

        var lazy = _inflight.GetOrAdd(
            imageSource,
            key => new Lazy<Task<string>>(
                () => CacheImageAsync(key, uri, cancellationToken),
                LazyThreadSafetyMode.ExecutionAndPublication));

        return CompleteAndReleaseAsync(imageSource, lazy);
    }

    private async Task<string> CompleteAndReleaseAsync(
        string imageSource,
        Lazy<Task<string>> lazy)
    {
        try
        {
            return await lazy.Value.ConfigureAwait(false);
        }
        finally
        {
            _inflight.TryRemove(imageSource, out _);
        }
    }

    private async Task<string> CacheImageAsync(
        string imageSource,
        Uri uri,
        CancellationToken cancellationToken)
    {
        try
        {
            var cachePath = BuildCachePath(imageSource, uri);
            if (File.Exists(cachePath) && new FileInfo(cachePath).Length > 0)
            {
                return cachePath;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);

            using var response = await _httpClient.GetAsync(
                uri,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var tempPath = $"{cachePath}.tmp";
            await using (var source = await response.Content
                .ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false))
            await using (var target = File.Create(tempPath))
            {
                await source.CopyToAsync(target, cancellationToken).ConfigureAwait(false);
            }

            if (File.Exists(cachePath))
            {
                File.Delete(cachePath);
            }

            File.Move(tempPath, cachePath);
            return cachePath;
        }
        catch
        {
            return imageSource;
        }
    }

    private static string BuildCachePath(string imageSource, Uri uri)
    {
        var extension = Path.GetExtension(uri.AbsolutePath);
        if (string.IsNullOrWhiteSpace(extension) || !KnownExtensions.Contains(extension))
        {
            extension = ".img";
        }

        var fileName = $"{Hash(imageSource)}{extension.ToLowerInvariant()}";
        return Path.Combine(FileSystem.CacheDirectory, "product-images", fileName);
    }

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
