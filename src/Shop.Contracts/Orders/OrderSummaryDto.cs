namespace Shop.Contracts.Orders;

public sealed record OrderSummaryDto(Guid OrderId, string OrderNumber, decimal TotalAmount, string Currency, string Status);

