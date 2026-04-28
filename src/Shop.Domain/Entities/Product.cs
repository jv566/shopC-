using Shop.Domain.Common;
using Shop.Domain.ValueObjects;

namespace Shop.Domain.Entities;

public sealed class Product : Entity
{
    public Product(string name, string sku, Money price)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Name is required.", nameof(name)) : name.Trim();
        Sku = string.IsNullOrWhiteSpace(sku) ? throw new ArgumentException("SKU is required.", nameof(sku)) : sku.Trim().ToUpperInvariant();
        Price = price;
    }

    public string Name { get; private set; }

    public string Sku { get; private set; }

    public Money Price { get; private set; }

    public void ChangePrice(Money newPrice)
    {
        Price = newPrice;
    }
}

