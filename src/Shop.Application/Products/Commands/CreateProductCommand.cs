namespace Shop.Application.Products.Commands;

public sealed record CreateProductCommand(string Name, string Sku, decimal PriceAmount, string Currency);

