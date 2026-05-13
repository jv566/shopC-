using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Shop.Maui;

public partial class App : Microsoft.Maui.Controls.Application
{
    public App(IServiceProvider serviceProvider)
    {
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            LogError("InitComponent", ex);
        }

        try
        {
            MainPage = serviceProvider.GetRequiredService<Views.LoginPage>();
        }
        catch (Exception ex)
        {
            LogError("LoginPage", ex);
        }
    }

    private static void LogError(string stage, Exception ex)
    {
        try
        {
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "shop-maui-error.txt");

            File.WriteAllText(logPath, $"[{stage}] {DateTime.Now}\n{ex}\n\nInnerException: {ex.InnerException}");
        }
        catch { }
    }
}
