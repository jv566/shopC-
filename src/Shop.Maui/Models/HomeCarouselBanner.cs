namespace Shop.Maui.Models;

public sealed class HomeCarouselBanner
{
    public string Id { get; }
    public string Title { get; }
    public string? Description { get; }
    public string? ImagePath { get; }

    public HomeCarouselBanner(string id, string title, string? imagePath, string? description)
    {
        Id = id;
        Title = title;
        Description = description;
        ImagePath = imagePath;
    }
}
