namespace Shop.Maui.Models;

public sealed record CartLineItem(
    string ProductId,
    string CategoryId,
    string ProductType,
    string ModelName,
    decimal UnitPrice,
    int Quantity,
    string? ImageUrl)
{
    public decimal Subtotal => UnitPrice * Quantity;

    public string UnitPriceText => FormatCurrency(UnitPrice);

    public string SubtotalText => FormatCurrency(Subtotal);

    public string QuantityText => $"x{Quantity}";

    private static string FormatCurrency(decimal value) => $"￥{value:F2}";
}
