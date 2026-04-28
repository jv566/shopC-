using System;
using System.IO;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using Shop.Desktop.Configuration;
using Shop.Desktop.Services;

namespace Shop.Desktop.Views.Pages;

public partial class ProductPanoramaReplacementView : UserControl
{
    private const string VrHouseVirtualHost = "appassets.example";
    private const string VrHouseRelativeIndexPath = "Web\\VrHouse\\index.html";
    private const string VrHouseHostUrl = "https://appassets.example/Web/VrHouse/index.html";

    private const string Product3DRelativeIndexPath = "Web\\Product3D\\index.html";
    private const string Product3DHostUrl = "https://appassets.example/Web/Product3D/index.html";

    private bool _isWebViewInitialized;

    public ProductPanoramaReplacementView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnBackClick(object sender, System.Windows.RoutedEventArgs e)
    {
        DesktopServices.Navigation.NavigateToHome();
    }

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_isWebViewInitialized)
        {
            return;
        }

        _isWebViewInitialized = true;

        await PanoramaWebView.EnsureCoreWebView2Async();
        await PanoramaProduct3DWebView.EnsureCoreWebView2Async();

        var appFolder = AppContext.BaseDirectory;
        var vrHouseIndexPath = Path.Combine(appFolder, VrHouseRelativeIndexPath);
        var product3DIndexPath = Path.Combine(appFolder, Product3DRelativeIndexPath);

        if (File.Exists(vrHouseIndexPath))
        {
            PanoramaWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                VrHouseVirtualHost,
                appFolder,
                CoreWebView2HostResourceAccessKind.Allow);

            var version = File.GetLastWriteTimeUtc(vrHouseIndexPath).Ticks;
            PanoramaWebView.Source = new Uri($"{VrHouseHostUrl}?v={version}");
        }
        else
        {
            await WebViewContentLoader.InitializeAsync(
                PanoramaWebView,
                PanoramaWebViewSettings.PanoramaUrl,
                "全景场景预览占位",
                PanoramaWebViewSettings.PanoramaUrlPath);
        }

        if (File.Exists(product3DIndexPath))
        {
            PanoramaProduct3DWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                VrHouseVirtualHost,
                appFolder,
                CoreWebView2HostResourceAccessKind.Allow);

            var version = File.GetLastWriteTimeUtc(product3DIndexPath).Ticks;
            PanoramaProduct3DWebView.Source = new Uri($"{Product3DHostUrl}?v={version}");
            return;
        }

        PanoramaProduct3DWebView.NavigateToString(Build3DPlaceholderHtml(product3DIndexPath));
    }

    private static string Build3DPlaceholderHtml(string filePath)
    {
        return $$"""
<!DOCTYPE html>
<html lang="zh-CN">
<head>
  <meta charset="utf-8" />
  <title>3D 预览占位</title>
  <style>
    body { font-family: 'Microsoft YaHei', sans-serif; margin: 0; display: grid; place-items: center; height: 100vh; color: #333; background: #f6f7f9; }
    .card { border: 1px solid #bbb; background: #fff; padding: 14px 16px; border-radius: 8px; max-width: 460px; }
    h3 { margin: 0 0 8px; font-size: 18px; }
    p { margin: 4px 0; font-size: 13px; line-height: 1.6; }
    code { background: #f1f3f5; padding: 2px 6px; border-radius: 4px; }
  </style>
</head>
<body>
  <div class="card">
    <h3>3D 预览文件未找到</h3>
    <p>请确认 3D 页面已复制到:</p>
    <p><code>{{filePath}}</code></p>
  </div>
</body>
</html>
""";
    }
}
