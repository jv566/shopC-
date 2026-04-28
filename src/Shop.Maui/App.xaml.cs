using System.Diagnostics;

namespace Shop.Maui;

public partial class App : Microsoft.Maui.Controls.Application
{
    public App()
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
            MainPage = new AppShell();
        }
        catch (Exception ex)
        {
            LogError("AppShell", ex);
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
