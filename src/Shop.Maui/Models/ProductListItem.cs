namespace Shop.Maui.Models;

public sealed record ProductListItem(
    string Id,
    string CategoryId,
    string ModelName,
    decimal SalePrice,
    string? ImageUrl);
