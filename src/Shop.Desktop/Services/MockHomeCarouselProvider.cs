using Shop.Desktop.Models;

namespace Shop.Desktop.Services;

public sealed class MockHomeCarouselProvider : IHomeCarouselProvider
{
    // Temporary local placeholders. Replace with backend API data later.
    private static readonly IReadOnlyList<HomeCarouselBanner> Banners = new List<HomeCarouselBanner>
    {
        new("banner-1", "主推卧室场景", null, "轮播图占位：未来显示后端返回图片"),
        new("banner-2", "客厅组合推荐", null, "轮播图占位：支持图片 URL 动态加载"),
        new("banner-3", "餐厅新品精选", null, "轮播图占位：后续接入 CDN 图片")
    };

    public Task<IReadOnlyList<HomeCarouselBanner>> GetBannersAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Banners);
    }
}
