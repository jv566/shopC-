using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Wpf;

namespace Shop.Desktop.Configuration;

public static class PanoramaWebViewSettings
{
    public const string ConfigFileName = "appsettings.json";
    public const string ConfigSectionName = "PanoramaWebView";

    public const string PanoramaUrlKey = "PanoramaUrl";
    public const string Home3DUrlKey = "Home3DUrl";
    public const string HomePanoramaUrlKey = "HomePanoramaUrl";

    public const string HomeCarouselAutoPlayEnabledKey = "HomeCarouselAutoPlayEnabled";
    public const string HomeCarouselIntervalSecondsKey = "HomeCarouselIntervalSeconds";
    public const string HomeCarouselSlideDurationMsKey = "HomeCarouselSlideDurationMs";

    public const string PanoramaUrlPath = ConfigSectionName + ":" + PanoramaUrlKey;
    public const string Home3DUrlPath = ConfigSectionName + ":" + Home3DUrlKey;
    public const string HomePanoramaUrlPath = ConfigSectionName + ":" + HomePanoramaUrlKey;

    private static readonly Lazy<Snapshot> SnapshotValue = new(LoadSnapshot);

    public static string? PanoramaUrl => SnapshotValue.Value.PanoramaUrl;

    public static string? Home3DUrl => SnapshotValue.Value.Home3DUrl;

    public static string? HomePanoramaUrl => SnapshotValue.Value.HomePanoramaUrl ?? PanoramaUrl;

    public static bool HomeCarouselAutoPlayEnabled => SnapshotValue.Value.HomeCarouselAutoPlayEnabled;

    public static int HomeCarouselIntervalSeconds => SnapshotValue.Value.HomeCarouselIntervalSeconds;

    public static int HomeCarouselSlideDurationMs => SnapshotValue.Value.HomeCarouselSlideDurationMs;

    private static Snapshot LoadSnapshot()
    {
        var snapshot = new Snapshot
        {
            HomeCarouselAutoPlayEnabled = true,
            HomeCarouselIntervalSeconds = 4,
            HomeCarouselSlideDurationMs = 280
        };

        var filePath = Path.Combine(AppContext.BaseDirectory, ConfigFileName);
        if (!File.Exists(filePath))
        {
            return snapshot;
        }

        try
        {
            using var stream = File.OpenRead(filePath);
            using var doc = JsonDocument.Parse(stream);

            if (!doc.RootElement.TryGetProperty(ConfigSectionName, out var section) ||
                section.ValueKind != JsonValueKind.Object)
            {
                return snapshot;
            }

            snapshot.PanoramaUrl = ReadString(section, PanoramaUrlKey);
            snapshot.Home3DUrl = ReadString(section, Home3DUrlKey);
            snapshot.HomePanoramaUrl = ReadString(section, HomePanoramaUrlKey);

            snapshot.HomeCarouselAutoPlayEnabled = ReadBool(
                section,
                HomeCarouselAutoPlayEnabledKey,
                snapshot.HomeCarouselAutoPlayEnabled);

            snapshot.HomeCarouselIntervalSeconds = ReadInt(
                section,
                HomeCarouselIntervalSecondsKey,
                snapshot.HomeCarouselIntervalSeconds,
                min: 1,
                max: 60);

            snapshot.HomeCarouselSlideDurationMs = ReadInt(
                section,
                HomeCarouselSlideDurationMsKey,
                snapshot.HomeCarouselSlideDurationMs,
                min: 80,
                max: 5000);
        }
        catch
        {
            // Keep defaults when config file has invalid format.
        }

        return snapshot;
    }

    private static string? ReadString(JsonElement section, string key)
    {
        if (!section.TryGetProperty(key, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            var text = value.GetString();
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }

        return null;
    }

    private static bool ReadBool(JsonElement section, string key, bool defaultValue)
    {
        if (!section.TryGetProperty(key, out var value))
        {
            return defaultValue;
        }

        if (value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False)
        {
            return value.GetBoolean();
        }

        if (value.ValueKind == JsonValueKind.String && bool.TryParse(value.GetString(), out var parsed))
        {
            return parsed;
        }

        return defaultValue;
    }

    private static int ReadInt(JsonElement section, string key, int defaultValue, int min, int max)
    {
        if (!section.TryGetProperty(key, out var value))
        {
            return defaultValue;
        }

        var parsed = defaultValue;

        if (value.ValueKind == JsonValueKind.Number)
        {
            if (!value.TryGetInt32(out parsed))
            {
                return defaultValue;
            }
        }
        else if (value.ValueKind == JsonValueKind.String)
        {
            if (!int.TryParse(value.GetString(), out parsed))
            {
                return defaultValue;
            }
        }
        else
        {
            return defaultValue;
        }

        if (parsed < min)
        {
            return min;
        }

        if (parsed > max)
        {
            return max;
        }

        return parsed;
    }

    private sealed class Snapshot
    {
        public string? PanoramaUrl { get; set; }

        public string? Home3DUrl { get; set; }

        public string? HomePanoramaUrl { get; set; }

        public bool HomeCarouselAutoPlayEnabled { get; set; }

        public int HomeCarouselIntervalSeconds { get; set; }

        public int HomeCarouselSlideDurationMs { get; set; }
    }
}

public static class WebViewContentLoader
{
    public static async Task InitializeAsync(WebView2 webView, string? rawUrl, string placeholderTitle, string configPath)
    {
        await webView.EnsureCoreWebView2Async();

        if (TryGetUri(rawUrl, out var uri))
        {
            webView.Source = uri;
            return;
        }

        webView.NavigateToString(BuildPlaceholderHtml(placeholderTitle, configPath));
    }

    private static bool TryGetUri(string? rawUrl, out Uri uri)
    {
        uri = null!;

        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            return false;
        }

        if (!Uri.TryCreate(rawUrl.Trim(), UriKind.Absolute, out var result))
        {
            return false;
        }

        if (result.Scheme != Uri.UriSchemeHttp && result.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        uri = result;
        return true;
    }

    private static string BuildPlaceholderHtml(string title, string configPath)
    {
        return $$"""
<!DOCTYPE html>
<html lang="zh-CN">
<head>
  <meta charset="utf-8" />
  <title>{{title}}</title>
  <style>
    body { font-family: 'Microsoft YaHei', sans-serif; margin: 0; display: grid; place-items: center; height: 100vh; color: #333; background: #f6f7f9; }
    .card { border: 1px solid #bbb; background: #fff; padding: 16px 20px; border-radius: 8px; max-width: 520px; }
    h2 { margin: 0 0 8px; font-size: 20px; }
    p { margin: 5px 0; font-size: 14px; line-height: 1.6; }
    code { background: #f1f3f5; padding: 2px 6px; border-radius: 4px; }
  </style>
</head>
<body>
  <div class="card">
    <h2>{{title}}</h2>
    <p>当前未配置外接网站地址。</p>
    <p>请在 <code>{{PanoramaWebViewSettings.ConfigFileName}}</code> 中配置 <code>{{configPath}}</code>。</p>
  </div>
</body>
</html>
""";
    }
}
