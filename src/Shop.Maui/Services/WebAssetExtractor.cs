namespace Shop.Maui.Services;

public static class WebAssetExtractor
{
    private static readonly string[] VrHouseFiles = new[]
    {
        "Web/VrHouse/index.html",
        "Web/VrHouse/favicon.ico",
        "Web/VrHouse/assets/index-AXK8wDuV.js",
        "Web/VrHouse/assets/index-CSqmUKuP.css",
        "Web/VrHouse/models/CB3707沙发.glb",
        "Web/VrHouse/panoramas/bedroom/room2.jpg",
        "Web/VrHouse/panoramas/kitchen/room3.jpg",
        "Web/VrHouse/panoramas/living-room/VR-1-3000x1500.jpg",
        "Web/VrHouse/panoramas/living-room/VR-1-7000x3500.jpg",
        "Web/VrHouse/panoramas/living-room/VR-1-7000x3500_b.jpg",
        "Web/VrHouse/panoramas/living-room/VR-1-7000x3500_d.jpg",
        "Web/VrHouse/panoramas/living-room/VR-1-7000x3500_f.jpg",
        "Web/VrHouse/panoramas/living-room/VR-1-7000x3500_l.jpg",
        "Web/VrHouse/panoramas/living-room/VR-1-7000x3500_r.jpg",
        "Web/VrHouse/panoramas/living-room/VR-1-7000x3500_u.jpg",
    };

    private static readonly string[] Product3DFiles = new[]
    {
        "Web/Product3D/index.html",
        "Web/Product3D/sofa.glb",
    };

    public static async Task ExtractVrHouseAsync()
    {
        await ExtractFilesAsync(VrHouseFiles);
    }

    public static async Task ExtractProduct3DAsync()
    {
        await ExtractFilesAsync(Product3DFiles);
    }

    private static async Task ExtractFilesAsync(string[] fileList)
    {
        var cacheDir = FileSystem.CacheDirectory;

        foreach (var assetPath in fileList)
        {
            var normalizedPath = assetPath.Replace('/', Path.DirectorySeparatorChar);
            var targetPath = Path.Combine(cacheDir, normalizedPath);
            var targetDir = Path.GetDirectoryName(targetPath)!;

            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            if (File.Exists(targetPath))
            {
                continue;
            }

            try
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync(assetPath);
                using var fileStream = File.Create(targetPath);
                await stream.CopyToAsync(fileStream);
            }
            catch (FileNotFoundException)
            {
                // 资源可能未打包，跳过
            }
        }
    }

    public static string GetVrHouseIndexUrl()
    {
        var path = Path.Combine(FileSystem.CacheDirectory, "Web", "VrHouse", "index.html");
        return new Uri(path).AbsoluteUri;
    }

    public static string GetProduct3DIndexUrl()
    {
        var path = Path.Combine(FileSystem.CacheDirectory, "Web", "Product3D", "index.html");
        return new Uri(path).AbsoluteUri;
    }
}
