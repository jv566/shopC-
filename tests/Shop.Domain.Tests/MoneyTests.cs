using Shop.Domain.ValueObjects;

namespace Shop.Domain.Tests;

public sealed class MoneyTests
{
    [Fact]
    public void Ctor_ShouldThrow_WhenAmountIsNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Money(-1, "CNY"));
    }

    [Fact]
    public void Ctor_ShouldNormalizeCurrency()
    {
        var money = new Money(12.345m, " cny ");

        Assert.Equal(12.35m, money.Amount);
        Assert.Equal("CNY", money.Currency);
    }
}

