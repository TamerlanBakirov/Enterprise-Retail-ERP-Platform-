using FluentAssertions;
using GeorgiaERP.Application.Organization.Commands;
using GeorgiaERP.Domain.Organization;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GeorgiaERP.Tests.Application;

public class OrganizationCommandHandlerTests
{
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"org-{Guid.NewGuid()}")
            .Options);

    // === CreateCompany ===

    [Fact]
    public async Task CreateCompany_Valid_ReturnsSuccess()
    {
        await using var db = NewContext();
        var handler = new CreateCompanyCommandHandler(db);

        var result = await handler.Handle(new CreateCompanyCommand(
            "COMP-001", "Test Company", "ტესტი კომპანია",
            "123456789", true, "Legal St", "Actual St",
            "+995555000000", "info@test.ge"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("COMP-001");
        result.Value.Tin.Should().Be("123456789");
        (await db.Companies.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CreateCompany_DuplicateCode_ReturnsFailure()
    {
        await using var db = NewContext();
        db.Companies.Add(Company.Create("COMP-DUP", "Existing", "111111111"));
        await db.SaveChangesAsync();

        var handler = new CreateCompanyCommandHandler(db);
        var result = await handler.Handle(new CreateCompanyCommand(
            "COMP-DUP", "New", null, "222222222", false, null, null, null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
    }

    [Fact]
    public async Task CreateCompany_DuplicateTin_ReturnsFailure()
    {
        await using var db = NewContext();
        db.Companies.Add(Company.Create("COMP-A", "Existing", "999888777"));
        await db.SaveChangesAsync();

        var handler = new CreateCompanyCommandHandler(db);
        var result = await handler.Handle(new CreateCompanyCommand(
            "COMP-B", "New Company", null, "999888777", false, null, null, null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("TIN");
    }

    // === UpdateCompany ===

    [Fact]
    public async Task UpdateCompany_Valid_Changes()
    {
        await using var db = NewContext();
        var company = Company.Create("COMP-UPD", "Original", "555666777");
        db.Companies.Add(company);
        await db.SaveChangesAsync();

        var handler = new UpdateCompanyCommandHandler(db);
        var result = await handler.Handle(new UpdateCompanyCommand(
            company.Id, "Updated Name", "განახლებული",
            "New Legal", "New Actual", "+995555111222", "new@test.ge"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var saved = await db.Companies.FindAsync(company.Id);
        saved!.Name.Should().Be("Updated Name");
        saved.Email.Should().Be("new@test.ge");
    }

    [Fact]
    public async Task UpdateCompany_NotFound_ReturnsFailure()
    {
        await using var db = NewContext();
        var handler = new UpdateCompanyCommandHandler(db);

        var result = await handler.Handle(new UpdateCompanyCommand(
            Guid.NewGuid(), "Name", null, null, null, null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    // === CreateStore ===

    [Fact]
    public async Task CreateStore_Valid_ReturnsSuccess()
    {
        await using var db = NewContext();
        var handler = new CreateStoreCommandHandler(db);

        var result = await handler.Handle(new CreateStoreCommand(
            "STR-001", "Main Store", "მთავარი მაღაზია", "Retail",
            "123 Main St", "Tbilisi", "Tbilisi", "+995555123456", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("STR-001");
        result.Value.StoreType.Should().Be("Retail");
        (await db.Stores.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CreateStore_DuplicateCode_ReturnsFailure()
    {
        await using var db = NewContext();
        db.Stores.Add(Store.Create("STR-DUP", "Existing", StoreType.Retail));
        await db.SaveChangesAsync();

        var handler = new CreateStoreCommandHandler(db);
        var result = await handler.Handle(new CreateStoreCommand(
            "STR-DUP", "New Store", null, "Retail", null, null, null, null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
    }

    [Fact]
    public async Task CreateStore_InvalidType_ReturnsFailure()
    {
        await using var db = NewContext();
        var handler = new CreateStoreCommandHandler(db);

        var result = await handler.Handle(new CreateStoreCommand(
            "STR-BAD", "Bad Type", null, "InvalidType", null, null, null, null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid store type");
    }

    // === UpdateStore ===

    [Fact]
    public async Task UpdateStore_Valid_Changes()
    {
        await using var db = NewContext();
        var store = Store.Create("STR-UPD", "Original", StoreType.Retail);
        db.Stores.Add(store);
        await db.SaveChangesAsync();

        var handler = new UpdateStoreCommandHandler(db);
        var result = await handler.Handle(new UpdateStoreCommand(
            store.Id, "Updated Store", "განახლებული", "New Address",
            "Batumi", "Adjara", "+995555999888", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var saved = await db.Stores.FindAsync(store.Id);
        saved!.Name.Should().Be("Updated Store");
        saved.City.Should().Be("Batumi");
    }

    [Fact]
    public async Task UpdateStore_NotFound_ReturnsFailure()
    {
        await using var db = NewContext();
        var handler = new UpdateStoreCommandHandler(db);

        var result = await handler.Handle(new UpdateStoreCommand(
            Guid.NewGuid(), "Name", null, null, null, null, null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
