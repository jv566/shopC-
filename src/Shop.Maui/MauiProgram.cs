using Microsoft.Extensions.Logging;
using Shop.Maui.Services;
using Shop.Maui.ViewModels;
using Shop.Maui.Views;

namespace Shop.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Services
        builder.Services.AddSingleton<INavigationService, NavigationService>();
        builder.Services.AddSingleton<IHomeCarouselProvider, MockHomeCarouselProvider>();
        builder.Services.AddSingleton<IProductCategoryProvider, MockProductCategoryProvider>();
        builder.Services.AddSingleton<IProductCategoryTreeProvider, MockProductCategoryTreeProvider>();
        builder.Services.AddSingleton<IProductProvider, MockProductProvider>();
        builder.Services.AddSingleton<IProductColorVariantProvider, MockProductColorVariantProvider>();
        builder.Services.AddSingleton<IUserActionService, MockUserActionService>();

        // ViewModels
        builder.Services.AddTransient<HomePageViewModel>();
        builder.Services.AddTransient<ProductListViewModel>();
        builder.Services.AddTransient<ProductDetailViewModel>();
        builder.Services.AddTransient<Showcase3DViewModel>();

        // Pages
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<Image2CategoryPage>();
        builder.Services.AddTransient<ProductDetailPage>();
        builder.Services.AddTransient<PanoramaPage>();
        builder.Services.AddTransient<Showcase3DPage>();

        return builder.Build();
    }
}
