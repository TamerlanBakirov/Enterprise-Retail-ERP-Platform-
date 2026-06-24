using FluentAssertions;
using GeorgiaERP.Application.Organization.Queries;
using GeorgiaERP.Domain.Organization;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GeorgiaERP.Tests.Application;

public class GetStoreByIdQueryTests
{
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"store-byid-{Guid.NewGuid()}")
            .Options);

    [Fact]
    public async Task GetById_ExistingStore_ReturnsDto()
    {
        await using var db = NewContext();
        var store = Store.Create("ST-01", "Rustaveli Flagship", StoreType.Retail, "რუსთაველი");
        db.Stores.Add(store);
        await db.SaveChangesAsync();

        var result = await new GetStoreByIdQueryHandler(db)
            .Handle(new GetStoreByIdQuery(store.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(store.Id);
        result.Value.Code.Should().Be("ST-01");
        result.Value.StoreType.Should().Be("Retail");
    }

    [Fact]
    public async Task GetById_Unknown_ReturnsNotFound()
    {
        await using var db = NewContext();

        var result = await new GetStoreByIdQueryHandler(db)
            .Handle(new GetStoreByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("NOT_FOUND");
    }
}
