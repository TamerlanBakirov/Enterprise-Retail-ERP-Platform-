using FluentAssertions;
using GeorgiaERP.Application.Procurement.Queries;
using GeorgiaERP.Domain.Procurement;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GeorgiaERP.Tests.Application;

public class GetSupplierByIdQueryTests
{
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"supplier-byid-{Guid.NewGuid()}")
            .Options);

    [Fact]
    public async Task GetById_ExistingSupplier_ReturnsDto()
    {
        await using var db = NewContext();
        var supplier = Supplier.Create("SUP-9", "Tbilisi Wholesale");
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync();

        var result = await new GetSupplierByIdQueryHandler(db)
            .Handle(new GetSupplierByIdQuery(supplier.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(supplier.Id);
        result.Value.Code.Should().Be("SUP-9");
        result.Value.Name.Should().Be("Tbilisi Wholesale");
    }

    [Fact]
    public async Task GetById_Unknown_ReturnsNotFound()
    {
        await using var db = NewContext();

        var result = await new GetSupplierByIdQueryHandler(db)
            .Handle(new GetSupplierByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("NOT_FOUND");
    }
}
