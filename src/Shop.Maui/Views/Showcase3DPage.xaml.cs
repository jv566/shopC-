namespace Shop.Maui.Views;

public partial class Showcase3DPage : ContentPage
{
    public Showcase3DPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // TODO: 将 WPF 的 Web/Product3D 资源放入 Resources/Raw 后，通过本地路径加载
        // ShowcaseWebView.Source = "webview/product3d/index.html";
        ShowcaseWebView.Source = new HtmlWebViewSource
        {
            Html = @"<!DOCTYPE html>
<html>
<head><meta charset='utf-8'><title>3D Showcase</title></head>
<body style='margin:0;display:flex;align-items:center;justify-content:center;height:100vh;background:#222;color:#fff;font-family:sans-serif;'>
<div style='text-align:center;'>
<h2>3D 展示占位</h2>
<p>请将 Product3D 资源放入 Resources/Raw 并配置本地加载</p>
<p>建议方案：使用 Babylon.js / Three.js 在 WebView 中渲染</p>
</div>
</body></html>"
        };
    }
}
