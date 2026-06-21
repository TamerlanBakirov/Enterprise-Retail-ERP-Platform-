using FluentAssertions;
using GeorgiaERP.Domain.Organization;
using Xunit;

namespace GeorgiaERP.Tests.Domain;

public class OrganizationTests
{
    // === Company ===

    [Fact]
    public void CreateCompany_SetsDefaultValues()
    {
        var company = Company.Create("COMP-001", "Georgia Trading LLC", "123456789", true, "საქართველოს ტრეიდინგი");

        company.Code.Should().Be("COMP-001");
        company.Name.Should().Be("Georgia Trading LLC");
        company.NameKa.Should().Be("საქართველოს ტრეიდინგი");
        company.Tin.Should().Be("123456789");
        company.IsVatPayer.Should().BeTrue();
        company.IsActive.Should().BeTrue();
    }

    [Fact]
    public void CreateCompany_WithoutVat_DefaultsFalse()
    {
        var company = Company.Create("COMP-001", "Small Shop", "987654321");

        company.IsVatPayer.Should().BeFalse();
        company.NameKa.Should().BeNull();
    }

    // === Store ===

    [Fact]
    public void CreateStore_SetsDefaultValues()
    {
        var store = Store.Create("STR-001", "Main Store", StoreType.Retail, "მთავარი მაღაზია");

        store.Code.Should().Be("STR-001");
        store.Name.Should().Be("Main Store");
        store.NameKa.Should().Be("მთავარი მაღაზია");
        store.StoreType.Should().Be(StoreType.Retail);
        store.IsActive.Should().BeTrue();
        store.Timezone.Should().Be("Asia/Tbilisi");
        store.ManagerUserId.Should().BeNull();
        store.Address.Should().BeNull();
        store.Latitude.Should().BeNull();
        store.Longitude.Should().BeNull();
    }

    [Theory]
    [InlineData(StoreType.Retail)]
    [InlineData(StoreType.Outlet)]
    [InlineData(StoreType.Franchise)]
    public void Store_AllTypes_CanBeCreated(StoreType type)
    {
        var store = Store.Create("STR-001", "Test", type);

        store.StoreType.Should().Be(type);
    }

    // === Warehouse ===

    [Fact]
    public void CreateWarehouse_SetsDefaultValues()
    {
        var wh = Warehouse.Create("WH-001", "Central Warehouse", WarehouseType.Central);

        wh.Code.Should().Be("WH-001");
        wh.Name.Should().Be("Central Warehouse");
        wh.WarehouseType.Should().Be(WarehouseType.Central);
        wh.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData(WarehouseType.Central)]
    [InlineData(WarehouseType.Regional)]
    [InlineData(WarehouseType.Store)]
    public void Warehouse_AllTypes_CanBeCreated(WarehouseType type)
    {
        var wh = Warehouse.Create("WH-001", "Test", type);

        wh.WarehouseType.Should().Be(type);
    }
}
