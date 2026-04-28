using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Shop.Desktop.Models;

// Future-ready banner contract. ImageUrl will be provided by backend later.
public class HomeCarouselBanner
{
    public string Id { get; }
    public string Title { get; }
    public string? Description { get; }
    public ImageSource? ImageSource { get; }

    public HomeCarouselBanner(string id, string title, string? imagePath, string? description)
    {
        Id = id;
        Title = title;
        Description = description;

        if (!string.IsNullOrEmpty(imagePath))
        {
            ImageSource = new BitmapImage(new Uri(imagePath, UriKind.RelativeOrAbsolute));
        }
    }
}
