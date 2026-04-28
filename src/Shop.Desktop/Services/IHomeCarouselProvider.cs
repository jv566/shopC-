using Shop.Desktop.Models;

namespace Shop.Desktop.Services;

public interface IHomeCarouselProvider
{
    Task<IReadOnlyList<HomeCarouselBanner>> GetBannersAsync(CancellationToken cancellationToken = default);
}
