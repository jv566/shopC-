using Shop.Maui.Models;

namespace Shop.Maui.Services;

public interface IHomeCarouselProvider
{
    Task<IReadOnlyList<HomeCarouselBanner>> GetBannersAsync(CancellationToken cancellationToken = default);
}
