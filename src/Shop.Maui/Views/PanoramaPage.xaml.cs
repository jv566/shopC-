using Shop.Maui.Services;

namespace Shop.Maui.Views;

public partial class PanoramaPage : ContentPage
{
    public PanoramaPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await WebAssetExtractor.ExtractVrHouseAsync();
        var url = WebAssetExtractor.GetVrHouseIndexUrl();

        if (!string.IsNullOrEmpty(url) && File.Exists(new Uri(url).LocalPath))
        {
            PanoramaWebView.Source = url;
        }
        else
        {
            PanoramaWebView.Source = new HtmlWebViewSource
            {
                Html = @"<!DOCTYPE html>
<html>
<head><meta charset='utf-8'><title>VR Panorama</title></head>
<body style='margin:0;display:flex;align-items:center;justify-content:center;height:100vh;background:#222;color:#fff;font-family:sans-serif;'>
<div style='text-align:center;'>
<h2>VR 全景资源未找到</h2>
<p>请确保 VrHouse 资源已正确放入 Resources/Raw/Web/VrHouse</p>
</div>
</body></html>"
            };
        }
    }
}
