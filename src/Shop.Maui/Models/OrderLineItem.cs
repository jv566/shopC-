namespace Shop.Maui.Models;

public sealed record OrderLineItem(
    string ProductName,
    int Quantity,
    decimal UnitPrice)
{
    public string QuantityText => $"x{Quantity}";

    public string UnitPriceText => $"￥{UnitPrice:F2}";
}
