using Microsoft.Extensions.Logging; // 日志功能，比如 Debug 输出日志
using Shop.Maui.Services;           // 引入服务层
using Shop.Maui.ViewModels;         // 引入 ViewModel 层
using Shop.Maui.Views;              // 引入页面层

namespace Shop.Maui;

// static 表示静态类
// 静态类不能 new，只能直接通过类名调用
// MauiProgram 是整个应用启动入口配置类
public static class MauiProgram
{
    // 创建整个 MAUI App
    public static MauiApp CreateMauiApp()
    {
        // 创建一个 App 构建器 builder
        // builder 就像一个总配置器
        var builder = MauiApp.CreateBuilder();

        // 链式调用配置 App
        builder
            // 指定 App 的入口类
            // App.xaml.cs 就是整个程序入口
            .UseMauiApp<App>()

            // 配置字体
            .ConfigureFonts(fonts =>
            {
                // 添加普通字体
                fonts.AddFont(
                    "OpenSans-Regular.ttf",
                    "OpenSansRegular");

                // 添加加粗字体
                fonts.AddFont(
                    "OpenSans-Semibold.ttf",
                    "OpenSansSemibold");
            });

        // 只有 Debug 模式才开启日志
#if DEBUG

        // 把日志输出到调试窗口
        builder.Logging.AddDebug();

#endif

        // =========================
        // 注册服务（Services）
        // =========================

        // 注册导航服务
        // 页面跳转用
        builder.Services.AddSingleton<
            INavigationService,
            NavigationService>();

        // 注册首页轮播图服务
        // Mock 表示模拟数据（假数据）
        builder.Services.AddSingleton<
            IHomeCarouselProvider,
            MockHomeCarouselProvider>();

        // 注册首页分类服务
        builder.Services.AddSingleton<
            IProductCategoryProvider,
            MockProductCategoryProvider>();

        // 注册分类树服务（真实 HTTP 请求）
        builder.Services.AddSingleton<
            IProductCategoryTreeProvider,
            HttpProductCategoryTreeProvider>();

        // 注册商品服务（真实 HTTP 请求）
        builder.Services.AddSingleton<
            IProductProvider,
            HttpProductProvider>();

        builder.Services.AddSingleton<
            IImageCacheService,
            FileImageCacheService>();

        // 注册商品颜色服务
        builder.Services.AddSingleton<
            IProductColorVariantProvider,
            MockProductColorVariantProvider>();

        // 注册用户行为服务
        builder.Services.AddSingleton<
            IUserActionService,
            MockUserActionService>();

        // =========================
        // 注册 ViewModel
        // =========================

        // 首页 ViewModel
        builder.Services.AddTransient<HomePageViewModel>();

        // 商品列表页 ViewModel
        builder.Services.AddTransient<ProductListViewModel>();

        // 商品详情页 ViewModel
        builder.Services.AddTransient<ProductDetailViewModel>();

        // 3D 展示页 ViewModel
        builder.Services.AddTransient<Showcase3DViewModel>();

        // =========================
        // 注册页面（Views）
        // =========================

        // 首页
        builder.Services.AddTransient<HomePage>();

        // 图片分类页
        builder.Services.AddTransient<Image2CategoryPage>();

        // 商品详情页
        builder.Services.AddTransient<ProductDetailPage>();

        // 全景页
        builder.Services.AddTransient<PanoramaPage>();

        // 3D 展示页
        builder.Services.AddTransient<Showcase3DPage>();

        // 构建整个 App
        // 所有服务、页面、ViewModel 都组装完成
        return builder.Build();
    }
}
