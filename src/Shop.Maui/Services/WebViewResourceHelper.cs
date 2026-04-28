namespace Shop.Maui.Services;

public static class WebViewResourceHelper
{
    /// <summary>
    /// 将 MauiAsset 中的 Web 资源复制到应用缓存目录，并返回本地文件 URL。
    /// </summary>
    public static async Task<string> GetLocalWebUrlAsync(string assetRelativePath)
    {
        var fileName = Path.GetFileName(assetRelativePath);
        var cacheDir = FileSystem.CacheDirectory;
        var targetPath = Path.Combine(cacheDir, "webview", fileName);
        var targetDir = Path.GetDirectoryName(targetPath)!;

        if (!Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        if (!File.Exists(targetPath))
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync(assetRelativePath);
            using var fileStream = File.Create(targetPath);
            await stream.CopyToAsync(fileStream);
        }

        return new Uri(targetPath).AbsoluteUri;
    }

    /// <summary>
    /// 将 MauiAsset 中的整个 Web 目录复制到应用缓存目录，并返回入口文件的本地文件 URL。
    /// </summary>
    public static async Task<string> CopyWebDirectoryAsync(string assetDirectory, string entryFileName)
    {
        var cacheDir = FileSystem.CacheDirectory;
        var targetDir = Path.Combine(cacheDir, "webview", assetDirectory.Replace('/', Path.DirectorySeparatorChar));

        if (!Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        // 由于 MAUI 的 MauiAsset 不支持枚举目录，我们采用已知文件列表方式
        // 或者通过 EmbeddedResource / 解压方式处理
        // 这里提供一个简化方案：只复制入口文件，其他资源通过相对路径加载
        var assetPath = $"{assetDirectory}/{entryFileName}".Replace(Path.DirectorySeparatorChar, '/');
        var entryTargetPath = Path.Combine(targetDir, entryFileName);

        if (!File.Exists(entryTargetPath))
        {
            try
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync(assetPath);
                using var fileStream = File.Create(entryTargetPath);
                await stream.CopyToAsync(fileStream);
            }
            catch (FileNotFoundException)
            {
                // 如果入口文件不存在，返回空字符串
                return string.Empty;
            }
        }

        return new Uri(entryTargetPath).AbsoluteUri;
    }
}
