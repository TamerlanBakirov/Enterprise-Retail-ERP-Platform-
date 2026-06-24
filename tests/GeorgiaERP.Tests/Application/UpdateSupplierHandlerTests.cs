using FluentAssertions;
using GeorgiaERP.Application.Procurement.Commands;
using GeorgiaERP.Domain.Procurement;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GeorgiaERP.Tests.Application;

public class UpdateSupplierHandlerTests
{
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"supplier-update-{Guid.NewGuid()}")
            .Options);

    private static async Task<Supplier> SeedSupplier(AppDbContext db)
    {
        var s = Supplier.Create("SUP-1", "Old Name", null, "111111111");
        db.Suppliers.Add(s);
        await db.SaveChangesAsync();
        return s;
    }

    private static UpdateSupplierCommand Cmd(Guid id, string name = "New Name", int? rating = 4, bool active = true) =>
        new(id, name, "ახალი", "222222222", IsVatPayer: true,
            "Jane", "+995599000000", "jane@supply.ge", "Tbilisi",
            "Net 30", 5000m, rating, active);

    [Fact]
    public async Task Update_Existing_AppliesChanges()
    {
        await using var db = NewContext();
        var s = await SeedSupplier(db);

        var result = await new UpdateSupplierCommandHandler(db)
            .Handle(Cmd(s.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("New Name");
        result.Value.Tin.Should().Be("222222222");
        result.Value.IsVatPayer.Should().BeTrue();
        result.Value.Rating.Should().Be(4);
        result.Value.Code.Should().Be("SUP-1"); // code immutable
    }

    [Fact]
    public async Task Update_Deactivate_SetsInactive()
    {
        await using var db = NewContext();
        var s = await SeedSupplier(db);

        var result = await new UpdateSupplierCommandHandler(db)
            .Handle(Cmd(s.Id, active: false), CancellationToken.None);

        result.Value!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Update_InvalidRating_ReturnsFailure()
    {
        await using var db = NewContext();
        var s = await SeedSupplier(db);

        var result = await new UpdateSupplierCommandHandler(db)
            .Handle(Cmd(s.Id, rating: 9), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Rating");
    }

    [Fact]
    public async Task Update_Unknown_ReturnsNotFound()
    {
        await using var db = NewContext();

        var result = await new UpdateSupplierCommandHandler(db)
            .Handle(Cmd(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("NOT_FOUND");
    }
}
