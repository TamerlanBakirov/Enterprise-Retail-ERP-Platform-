using FluentAssertions;
using GeorgiaERP.Domain.Products;
using GeorgiaERP.Domain.Products.Events;
using Xunit;

namespace GeorgiaERP.Tests.Domain;

public class ProductTests
{
    private static readonly Guid CategoryId = Guid.NewGuid();

    private static Product NewProduct(
        string sku = "SKU-001",
        string name = "Test Product",
        bool vatApplicable = true,
        string unitOfMeasure = "PCS",
        decimal? minStockLevel = null,
        decimal? maxStockLevel = null) =>
        Product.Create(
            sku: sku,
            name: name,
            categoryId: CategoryId,
            unitOfMeasure: unitOfMeasure,
            vatApplicable: vatApplicable,
            minStockLevel: minStockLevel,
            maxStockLevel: maxStockLevel);

    // --- Creation ---

    [Fact]
    public void Create_SetsAllRequiredProperties()
    {
        var product = NewProduct();

        product.Sku.Should().Be("SKU-001");
        product.Name.Should().Be("Test Product");
        product.CategoryId.Should().Be(CategoryId);
        product.UnitOfMeasure.Should().Be("PCS");
        product.VatApplicable.Should().BeTrue();
        product.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_DefaultsOptionalPropertiesToNull()
    {
        var product = NewProduct();

        product.NameKa.Should().BeNull();
        product.Description.Should().BeNull();
        product.WeightKg.Should().BeNull();
        product.VolumeL.Should().BeNull();
        product.MinStockLevel.Should().BeNull();
        product.MaxStockLevel.Should().BeNull();
        product.ReorderPoint.Should().BeNull();
        product.ReorderQty.Should().BeNull();
        product.ExciseCode.Should().BeNull();
    }

    [Fact]
    public void Create_DefaultsTrackingFlagsToFalse()
    {
        var product = NewProduct();

        product.IsSerialized.Should().BeFalse();
        product.IsBatchTracked.Should().BeFalse();
        product.HasExpiry.Should().BeFalse();
    }

    [Fact]
    public void Create_WithOptionalParameters_SetsThemCorrectly()
    {
        var product = Product.Create(
            sku: "SKU-002",
            name: "Tracked Product",
            categoryId: CategoryId,
            unitOfMeasure: "KG",
            vatApplicable: false,
            nameKa: "ტესტი",
            description: "A test product",
            weightKg: 1.5m,
            volumeL: 2.0m,
            widthCm: 10m,
            heightCm: 20m,
            depthCm: 5m,
            minStockLevel: 10m,
            maxStockLevel: 100m,
            reorderPoint: 20m,
            reorderQty: 50m,
            isSerialized: true,
            isBatchTracked: true,
            hasExpiry: true);

        product.NameKa.Should().Be("ტესტი");
        product.Description.Should().Be("A test product");
        product.WeightKg.Should().Be(1.5m);
        product.VolumeL.Should().Be(2.0m);
        product.WidthCm.Should().Be(10m);
        product.HeightCm.Should().Be(20m);
        product.DepthCm.Should().Be(5m);
        product.MinStockLevel.Should().Be(10m);
        product.MaxStockLevel.Should().Be(100m);
        product.ReorderPoint.Should().Be(20m);
        product.ReorderQty.Should().Be(50m);
        product.IsSerialized.Should().BeTrue();
        product.IsBatchTracked.Should().BeTrue();
        product.HasExpiry.Should().BeTrue();
        product.VatApplicable.Should().BeFalse();
    }

    [Fact]
    public void Create_GeneratesUniqueId()
    {
        var p1 = NewProduct(sku: "A");
        var p2 = NewProduct(sku: "B");

        p1.Id.Should().NotBe(Guid.Empty);
        p2.Id.Should().NotBe(Guid.Empty);
        p1.Id.Should().NotBe(p2.Id);
    }

    [Fact]
    public void Create_RaisesProductCreatedEvent()
    {
        var product = NewProduct();

        product.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductCreatedEvent>();

        var evt = (ProductCreatedEvent)product.DomainEvents[0];
        evt.ProductId.Should().Be(product.Id);
        evt.Sku.Should().Be("SKU-001");
        evt.Name.Should().Be("Test Product");
        evt.CategoryId.Should().Be(CategoryId);
        evt.VatApplicable.Should().BeTrue();
    }

    // --- Variant management ---

    [Fact]
    public void Variants_InitializesAsEmptyCollection()
    {
        var product = NewProduct();
        product.Variants.Should().BeEmpty();
    }

    [Fact]
    public void Variants_CanAddVariant()
    {
        var product = NewProduct();
        var variant = ProductVariant.Create(product.Id, "SKU-001-RED", "Red Variant", "{\"color\":\"red\"}");

        product.Variants.Add(variant);

        product.Variants.Should().ContainSingle();
        var v = product.Variants.First();
        v.ProductId.Should().Be(product.Id);
        v.Sku.Should().Be("SKU-001-RED");
        v.Name.Should().Be("Red Variant");
        v.Attributes.Should().Be("{\"color\":\"red\"}");
        v.IsActive.Should().BeTrue();
        v.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Variants_CanAddMultipleVariants()
    {
        var product = NewProduct();
        product.Variants.Add(ProductVariant.Create(product.Id, "SKU-001-S", "Small"));
        product.Variants.Add(ProductVariant.Create(product.Id, "SKU-001-M", "Medium"));
        product.Variants.Add(ProductVariant.Create(product.Id, "SKU-001-L", "Large"));

        product.Variants.Should().HaveCount(3);
    }

    // --- Barcode management ---

    [Fact]
    public void Barcodes_InitializesAsEmptyCollection()
    {
        var product = NewProduct();
        product.Barcodes.Should().BeEmpty();
    }

    [Fact]
    public void Barcodes_CanAddBarcode_Ean13()
    {
        var product = NewProduct();
        var barcode = ProductBarcode.Create(product.Id, "4901234567890", BarcodeType.Ean13, isPrimary: true);

        product.Barcodes.Add(barcode);

        product.Barcodes.Should().ContainSingle();
        var bc = product.Barcodes.First();
        bc.Barcode.Should().Be("4901234567890");
        bc.BarcodeType.Should().Be(BarcodeType.Ean13);
        bc.IsPrimary.Should().BeTrue();
        bc.VariantId.Should().BeNull();
    }

    [Fact]
    public void Barcodes_CanAddBarcode_WithVariant()
    {
        var product = NewProduct();
        var variantId = Guid.NewGuid();
        var barcode = ProductBarcode.Create(product.Id, "INT-001-RED", BarcodeType.Internal, variantId: variantId);

        product.Barcodes.Add(barcode);

        product.Barcodes.First().VariantId.Should().Be(variantId);
    }

    [Theory]
    [InlineData(BarcodeType.Ean13)]
    [InlineData(BarcodeType.Ean8)]
    [InlineData(BarcodeType.Upc)]
    [InlineData(BarcodeType.Code128)]
    [InlineData(BarcodeType.Internal)]
    public void Barcodes_AllBarcodeTypes_AreSupported(BarcodeType type)
    {
        var barcode = ProductBarcode.Create(Guid.NewGuid(), "TEST-123", type);
        barcode.BarcodeType.Should().Be(type);
    }

    [Fact]
    public void Barcodes_MultipleBarcodes_OnlyOnePrimary()
    {
        var product = NewProduct();
        product.Barcodes.Add(ProductBarcode.Create(product.Id, "4901234567890", BarcodeType.Ean13, isPrimary: true));
        product.Barcodes.Add(ProductBarcode.Create(product.Id, "INT-SECONDARY", BarcodeType.Internal, isPrimary: false));

        product.Barcodes.Count(b => b.IsPrimary).Should().Be(1);
        product.Barcodes.Should().HaveCount(2);
    }

    // --- Product Bundle ---

    [Fact]
    public void ProductBundle_Create_SetsProperties()
    {
        var bundleProductId = Guid.NewGuid();
        var componentProductId = Guid.NewGuid();

        var bundle = ProductBundle.Create(bundleProductId, componentProductId, 3m);

        bundle.BundleProductId.Should().Be(bundleProductId);
        bundle.ComponentProductId.Should().Be(componentProductId);
        bundle.Quantity.Should().Be(3m);
    }

    // --- Category ---

    [Fact]
    public void Category_Create_SetsDefaults()
    {
        var category = Category.Create("CAT-001", "Electronics", nameKa: "ელექტრონიკა");

        category.Code.Should().Be("CAT-001");
        category.Name.Should().Be("Electronics");
        category.NameKa.Should().Be("ელექტრონიკა");
        category.ParentId.Should().BeNull();
        category.SortOrder.Should().Be(0);
        category.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Category_Create_WithParent()
    {
        var parentId = Guid.NewGuid();
        var category = Category.Create("CAT-002", "Smartphones", parentId: parentId, sortOrder: 5);

        category.ParentId.Should().Be(parentId);
        category.SortOrder.Should().Be(5);
    }
}
