using Shop.Maui.Services;

namespace Shop.Maui.Views;

public partial class Showcase3DPage : ContentPage
{
    public Showcase3DPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await WebAssetExtractor.ExtractProduct3DAsync();
        var url = WebAssetExtractor.GetProduct3DIndexUrl();

        if (!string.IsNullOrEmpty(url) && File.Exists(new Uri(url).LocalPath))
        {
            ShowcaseWebView.Source = url;
        }
        else
        {
            ShowcaseWebView.Source = new HtmlWebViewSource
            {
                Html = @"<!DOCTYPE html>
<html>
<head><meta charset='utf-8'><title>3D Showcase</title></head>
<body style='margin:0;display:flex;align-items:center;justify-content:center;height:100vh;background:#222;color:#fff;font-family:sans-serif;'>
<div style='text-align:center;'>
<h2>3D 展示资源未找到</h2>
<p>请确保 Product3D 资源已正确放入 Resources/Raw/Web/Product3D</p>
<p>建议方案：使用 Babylon.js / Three.js 在 WebView 中渲染</p>
</div>
</body></html>"
            };
        }
    }
}
