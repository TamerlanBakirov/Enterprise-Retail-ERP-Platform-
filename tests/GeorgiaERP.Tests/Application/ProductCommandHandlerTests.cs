using FluentAssertions;
using GeorgiaERP.Application.Products.Commands;
using GeorgiaERP.Application.Products.DTOs;
using GeorgiaERP.Domain.Products;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GeorgiaERP.Tests.Application;

public class ProductCommandHandlerTests
{
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"products-{Guid.NewGuid()}")
            .Options);

    private static async Task<Guid> SeedCategory(AppDbContext db, string code = "CAT-001")
    {
        var cat = Category.Create(code, "Test Category");
        db.Categories.Add(cat);
        await db.SaveChangesAsync();
        return cat.Id;
    }

    private static CreateProductCommand MakeCommand(
        Guid categoryId, string sku = "SKU-001", string name = "Test Product") =>
        new(sku, name, "ტესტი", "Description", categoryId, "Piece",
            true, 1.5m, null, null, null, null, 5m, 100m, 10m, 25m,
            false, false, false, null, Guid.NewGuid());

    // === CreateProduct ===

    [Fact]
    public async Task Create_ValidProduct_ReturnsSuccess()
    {
        await using var db = NewContext();
        var catId = await SeedCategory(db);
        var handler = new CreateProductCommandHandler(db);

        var result = await handler.Handle(MakeCommand(catId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Sku.Should().Be("SKU-001");
        result.Value.Name.Should().Be("Test Product");
        (await db.Products.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Create_WithBarcodes_SavesBarcodes()
    {
        await using var db = NewContext();
        var catId = await SeedCategory(db);
        var cmd = new CreateProductCommand(
            "SKU-BC", "Barcoded Product", null, null, catId, "Piece",
            true, null, null, null, null, null, null, null, null, null,
            false, false, false,
            [new CreateBarcodeRequest("1234567890123", "EAN13", true)],
            Guid.NewGuid());
        var handler = new CreateProductCommandHandler(db);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Barcodes.Should().HaveCount(1);
        (await db.ProductBarcodes.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Create_DuplicateSku_ReturnsFailure()
    {
        await using var db = NewContext();
        var catId = await SeedCategory(db);
        var handler = new CreateProductCommandHandler(db);

        await handler.Handle(MakeCommand(catId, "DUP-SKU"), CancellationToken.None);
        var result = await handler.Handle(MakeCommand(catId, "DUP-SKU", "Another Product"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
    }

    [Fact]
    public async Task Create_InvalidCategory_ReturnsFailure()
    {
        await using var db = NewContext();
        var handler = new CreateProductCommandHandler(db);

        var result = await handler.Handle(MakeCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Category not found");
    }

    // === UpdateProduct ===

    [Fact]
    public async Task Update_ExistingProduct_ChangesFields()
    {
        await using var db = NewContext();
        var catId = await SeedCategory(db);
        var product = Product.Create("SKU-UPD", "Original", catId, "Piece");
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var handler = new UpdateProductCommandHandler(db);
        var result = await handler.Handle(
            new UpdateProductCommand(product.Id, "Updated", "განახლებული", null, null, null, null, null, null, null, null, null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var saved = await db.Products.FindAsync(product.Id);
        saved!.Name.Should().Be("Updated");
        saved.NameKa.Should().Be("განახლებული");
    }

    [Fact]
    public async Task Update_NonExistentProduct_ReturnsFailure()
    {
        await using var db = NewContext();
        var handler = new UpdateProductCommandHandler(db);

        var result = await handler.Handle(
            new UpdateProductCommand(Guid.NewGuid(), "Name", null, null, null, null, null, null, null, null, null, null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Update_IsActive_DeactivatesProduct()
    {
        await using var db = NewContext();
        var catId = await SeedCategory(db);
        var product = Product.Create("SKU-DEACT", "Product", catId, "Piece");
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var handler = new UpdateProductCommandHandler(db);
        await handler.Handle(
            new UpdateProductCommand(product.Id, null, null, null, null, null, null, null, null, null, null, null, false),
            CancellationToken.None);

        var saved = await db.Products.FindAsync(product.Id);
        saved!.IsActive.Should().BeFalse();
    }

    // === DeleteProduct ===

    [Fact]
    public async Task Delete_ExistingProduct_SoftDeletes()
    {
        await using var db = NewContext();
        var catId = await SeedCategory(db);
        var product = Product.Create("SKU-DEL", "To Delete", catId, "Piece");
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var handler = new DeleteProductCommandHandler(db);
        var result = await handler.Handle(new DeleteProductCommand(product.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var saved = await db.Products.FindAsync(product.Id);
        saved!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_NonExistentProduct_ReturnsFailure()
    {
        await using var db = NewContext();
        var handler = new DeleteProductCommandHandler(db);

        var result = await handler.Handle(new DeleteProductCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
