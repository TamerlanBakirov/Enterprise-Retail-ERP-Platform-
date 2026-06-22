using FluentAssertions;
using GeorgiaERP.Domain.Organization;
using Xunit;

namespace GeorgiaERP.Tests.Domain;

public class OrganizationTests
{
    [Fact]
    public void Company_Create_NonVatPayerByDefault()
    {
        var company = Company.Create("DEMO", "Demo Retail Georgia LLC", "405123456");

        company.Code.Should().Be("DEMO");
        company.Name.Should().Be("Demo Retail Georgia LLC");
        company.Tin.Should().Be("405123456");
        company.IsVatPayer.Should().BeFalse();
        company.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Company_Create_VatPayer_StoresFlagAndGeorgianName()
    {
        var company = Company.Create("DEMO", "Demo LLC", "405123456", isVatPayer: true, nameKa: "დემო");

        company.IsVatPayer.Should().BeTrue();
        company.NameKa.Should().Be("დემო");
    }

    [Fact]
    public void Store_Create_RetailType_IsActive()
    {
        var store = Store.Create("ST-TBS", "Tbilisi Central", StoreType.Retail, "თბილისი ცენტრალური");

        store.Code.Should().Be("ST-TBS");
        store.Name.Should().Be("Tbilisi Central");
        store.NameKa.Should().Be("თბილისი ცენტრალური");
        store.StoreType.Should().Be(StoreType.Retail);
        store.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData(StoreType.Retail)]
    [InlineData(StoreType.Outlet)]
    [InlineData(StoreType.Franchise)]
    public void Store_Create_SupportsAllStoreTypes(StoreType type)
    {
        var store = Store.Create("ST-X", "Store", type);

        store.StoreType.Should().Be(type);
    }
}
