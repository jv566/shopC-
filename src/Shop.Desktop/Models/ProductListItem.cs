namespace Shop.Desktop.Models;

public sealed record ProductListItem(
    string Id,
    string CategoryId,
    string ModelName,
    decimal SalePrice,
    string? ImageUrl);
