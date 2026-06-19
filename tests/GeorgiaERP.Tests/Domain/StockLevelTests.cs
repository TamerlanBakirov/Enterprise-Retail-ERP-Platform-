using FluentAssertions;
using GeorgiaERP.Domain.Inventory;
using Xunit;

namespace GeorgiaERP.Tests.Domain;

public class StockLevelTests
{
    [Fact]
    public void Create_InitializesRowVersion_SoInsertsSatisfyNotNull()
    {
        var stock = StockLevel.Create(Guid.NewGuid(), Guid.NewGuid(), costPrice: 5m);

        stock.RowVersion.Should().NotBeNull();
        stock.RowVersion.Should().NotBeEmpty();
    }

    [Fact]
    public void AddStock_IncreasesAvailableQuantity_AndBumpsRowVersion()
    {
        var stock = StockLevel.Create(Guid.NewGuid(), Guid.NewGuid());
        var before = stock.RowVersion;

        stock.AddStock(100m);

        stock.AvailableQuantity.Should().Be(100m);
        stock.RowVersion.Should().NotEqual(before);
    }

    [Fact]
    public void Deduct_ReducesAvailableQuantity()
    {
        var stock = StockLevel.Create(Guid.NewGuid(), Guid.NewGuid());
        stock.AddStock(100m);

        stock.Deduct(30m);

        stock.AvailableQuantity.Should().Be(70m);
    }

    [Fact]
    public void Deduct_NonPositive_Throws()
    {
        var stock = StockLevel.Create(Guid.NewGuid(), Guid.NewGuid());

        var act = () => stock.Deduct(0m);

        act.Should().Throw<InvalidOperationException>();
    }
}
