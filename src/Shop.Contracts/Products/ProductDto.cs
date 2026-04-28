namespace Shop.Contracts.Products;

public sealed record ProductDto(Guid Id, string Name, string Sku, decimal PriceAmount, string Currency);

