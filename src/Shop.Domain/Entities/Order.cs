using Shop.Domain.Common;
using Shop.Domain.Enums;
using Shop.Domain.ValueObjects;

namespace Shop.Domain.Entities;

public sealed class Order : Entity
{
    private readonly List<OrderLine> _lines = new();

    public Order(Guid customerId)
    {
        CustomerId = customerId;
        Status = OrderStatus.Draft;
    }

    public Guid CustomerId { get; }

    public OrderStatus Status { get; private set; }

    public IReadOnlyList<OrderLine> Lines => _lines;

    public Money TotalAmount =>
        new(_lines.Sum(x => x.UnitPrice.Amount * x.Quantity), _lines.Count == 0 ? "CNY" : _lines[0].UnitPrice.Currency);

    public void AddLine(Guid productId, int quantity, Money unitPrice)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        _lines.Add(new OrderLine(productId, quantity, unitPrice));
    }
}

public sealed record OrderLine(Guid ProductId, int Quantity, Money UnitPrice);

