using Shop.Application.Products.Commands;

namespace Shop.Application.Tests;

public sealed class CreateProductCommandTests
{
    [Fact]
    public void Ctor_ShouldStoreInputValues()
    {
        var command = new CreateProductCommand("Keyboard", "KB-001", 199, "CNY");

        Assert.Equal("Keyboard", command.Name);
        Assert.Equal("KB-001", command.Sku);
        Assert.Equal(199, command.PriceAmount);
        Assert.Equal("CNY", command.Currency);
    }
}

