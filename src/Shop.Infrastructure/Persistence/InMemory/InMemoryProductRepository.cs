using Shop.Application.Abstractions.Persistence;
using Shop.Domain.Entities;

namespace Shop.Infrastructure.Persistence.InMemory;

public sealed class InMemoryProductRepository : IProductRepository
{
    private readonly List<Product> _products =
    [
        new Product("Keyboard", "KB-001", new Domain.ValueObjects.Money(199, "CNY")),
        new Product("Mouse", "MS-001", new Domain.ValueObjects.Money(99, "CNY"))
    ];

    public Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Product>>(_products);
    }

    public Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        _products.Add(product);
        return Task.CompletedTask;
    }
}

