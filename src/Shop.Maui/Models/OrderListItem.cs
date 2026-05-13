namespace Shop.Maui.Models;

public sealed record OrderListItem(
    string OrderNo,
    string Status,
    DateTime CreatedAt,
    IReadOnlyList<OrderLineItem> Items)
{
    public int TotalQuantity => Items.Sum(item => item.Quantity);

    public decimal TotalAmount => Items.Sum(item => item.UnitPrice * item.Quantity);

    public string CreatedAtText => CreatedAt.ToString("yyyy-MM-dd HH:mm");

    public string SummaryText => $"共 {TotalQuantity} 件商品";

    public string TotalAmountText => $"￥{TotalAmount:F2}";
}
