namespace Shop.Maui.Views;

public partial class PanoramaPage : ContentPage
{
    public PanoramaPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // TODO: 将 WPF 的 Web/VrHouse 资源放入 Resources/Raw 后，通过本地路径加载
        // PanoramaWebView.Source = "webview/vrhouse/index.html";
        PanoramaWebView.Source = new HtmlWebViewSource
        {
            Html = @"<!DOCTYPE html>
<html>
<head><meta charset='utf-8'><title>VR Panorama</title></head>
<body style='margin:0;display:flex;align-items:center;justify-content:center;height:100vh;background:#222;color:#fff;font-family:sans-serif;'>
<div style='text-align:center;'>
<h2>VR 全景占位</h2>
<p>请将 VrHouse 资源放入 Resources/Raw 并配置本地加载</p>
</div>
</body></html>"
        };
    }
}
