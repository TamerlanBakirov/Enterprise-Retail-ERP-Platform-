using FluentAssertions;
using GeorgiaERP.Domain.Products;
using Xunit;

namespace GeorgiaERP.Tests.Domain;

public class ProductTests
{
    [Fact]
    public void Create_WithDefaults_IsActiveAndVatApplicable()
    {
        var product = Product.Create("SKU-001", "Borjomi 0.5L", Guid.NewGuid(), "pcs");

        product.Sku.Should().Be("SKU-001");
        product.Name.Should().Be("Borjomi 0.5L");
        product.UnitOfMeasure.Should().Be("pcs");
        product.VatApplicable.Should().BeTrue();
        product.IsActive.Should().BeTrue();
        product.IsSerialized.Should().BeFalse();
        product.IsBatchTracked.Should().BeFalse();
        product.HasExpiry.Should().BeFalse();
    }

    [Fact]
    public void Create_VatExempt_StoresFlag()
    {
        var product = Product.Create("SKU-002", "Postage Stamp", Guid.NewGuid(), "pcs", vatApplicable: false);

        product.VatApplicable.Should().BeFalse();
    }

    [Fact]
    public void Create_WithGeorgianNameAndDescription_StoresValues()
    {
        var product = Product.Create("SKU-003", "Khachapuri", Guid.NewGuid(), "pcs",
            nameKa: "ხაჭაპური", description: "Adjarian cheese bread");

        product.NameKa.Should().Be("ხაჭაპური");
        product.Description.Should().Be("Adjarian cheese bread");
    }

    [Fact]
    public void Create_PerishableBatchTracked_SetsTrackingFlags()
    {
        var product = Product.Create("SKU-004", "Fresh Milk", Guid.NewGuid(), "l",
            isBatchTracked: true, hasExpiry: true);

        product.IsBatchTracked.Should().BeTrue();
        product.HasExpiry.Should().BeTrue();
    }

    [Fact]
    public void Create_WithStockThresholds_StoresReorderConfig()
    {
        var product = Product.Create("SKU-005", "Coca-Cola 1L", Guid.NewGuid(), "pcs",
            minStockLevel: 10m, maxStockLevel: 500m, reorderPoint: 50m, reorderQty: 200m);

        product.MinStockLevel.Should().Be(10m);
        product.MaxStockLevel.Should().Be(500m);
        product.ReorderPoint.Should().Be(50m);
        product.ReorderQty.Should().Be(200m);
    }

    [Fact]
    public void Create_LinksToCategory()
    {
        var categoryId = Guid.NewGuid();

        var product = Product.Create("SKU-006", "Sulguni", categoryId, "kg");

        product.CategoryId.Should().Be(categoryId);
    }
}
