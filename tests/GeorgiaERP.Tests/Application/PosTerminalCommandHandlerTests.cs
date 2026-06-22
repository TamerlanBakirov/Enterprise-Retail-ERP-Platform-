using FluentAssertions;
using GeorgiaERP.Application.POS.Commands;
using GeorgiaERP.Domain.Organization;
using GeorgiaERP.Domain.POS;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GeorgiaERP.Tests.Application;

public class PosTerminalCommandHandlerTests
{
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"pos-terminal-{Guid.NewGuid()}")
            .Options);

    private static async Task<Guid> SeedStore(AppDbContext db, string code = "STR-POS")
    {
        var store = Store.Create(code, "POS Store", StoreType.Retail);
        db.Stores.Add(store);
        await db.SaveChangesAsync();
        return store.Id;
    }

    // === CreateTerminal ===

    [Fact]
    public async Task CreateTerminal_Valid_ReturnsSuccess()
    {
        await using var db = NewContext();
        var storeId = await SeedStore(db);
        var handler = new CreateTerminalCommandHandler(db);

        var result = await handler.Handle(new CreateTerminalCommand(
            "TERM-001", storeId, "Register 1", "Register"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("TERM-001");
        (await db.PosTerminals.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CreateTerminal_DuplicateCode_ReturnsFailure()
    {
        await using var db = NewContext();
        var storeId = await SeedStore(db);
        db.PosTerminals.Add(PosTerminal.Create("TERM-DUP", storeId, "Existing", TerminalType.Register));
        await db.SaveChangesAsync();

        var handler = new CreateTerminalCommandHandler(db);
        var result = await handler.Handle(new CreateTerminalCommand(
            "TERM-DUP", storeId, "New Terminal", "Register"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
    }

    [Fact]
    public async Task CreateTerminal_InvalidStore_ReturnsFailure()
    {
        await using var db = NewContext();
        var handler = new CreateTerminalCommandHandler(db);

        var result = await handler.Handle(new CreateTerminalCommand(
            "TERM-BAD", Guid.NewGuid(), "Terminal", "Register"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Store");
    }

    [Fact]
    public async Task CreateTerminal_InvalidType_ReturnsFailure()
    {
        await using var db = NewContext();
        var storeId = await SeedStore(db);
        var handler = new CreateTerminalCommandHandler(db);

        var result = await handler.Handle(new CreateTerminalCommand(
            "TERM-TYPE", storeId, "Terminal", "InvalidType"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid terminal type");
    }

    // === GetTerminals ===

    [Fact]
    public async Task GetTerminals_ReturnsAll()
    {
        await using var db = NewContext();
        var storeId = await SeedStore(db);
        db.PosTerminals.Add(PosTerminal.Create("T1", storeId, "Terminal 1", TerminalType.Register));
        db.PosTerminals.Add(PosTerminal.Create("T2", storeId, "Terminal 2", TerminalType.Mobile));
        await db.SaveChangesAsync();

        var handler = new GetTerminalsQueryHandler(db);
        var result = await handler.Handle(new GetTerminalsQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTerminals_FilterByStore()
    {
        await using var db = NewContext();
        var store1 = await SeedStore(db, "STR-1");
        var store2 = await SeedStore(db, "STR-2");
        db.PosTerminals.Add(PosTerminal.Create("T-S1", store1, "Store1 Terminal", TerminalType.Register));
        db.PosTerminals.Add(PosTerminal.Create("T-S2", store2, "Store2 Terminal", TerminalType.Register));
        await db.SaveChangesAsync();

        var handler = new GetTerminalsQueryHandler(db);
        var result = await handler.Handle(new GetTerminalsQuery(StoreId: store1), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Code.Should().Be("T-S1");
    }

    // === DailyClosing ===

    [Fact]
    public async Task DailyClosing_EmptyDay_ReturnsZeros()
    {
        await using var db = NewContext();
        var storeId = await SeedStore(db);

        var handler = new CreateDailyClosingCommandHandler(db);
        var result = await handler.Handle(new CreateDailyClosingCommand(
            storeId, DateTimeOffset.UtcNow, Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalSales.Should().Be(0);
        result.Value.TransactionCount.Should().Be(0);
        (await db.DailyClosings.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task DailyClosing_InvalidStore_ReturnsFailure()
    {
        await using var db = NewContext();
        var handler = new CreateDailyClosingCommandHandler(db);

        var result = await handler.Handle(new CreateDailyClosingCommand(
            Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task DailyClosing_DuplicateDate_ReturnsFailure()
    {
        await using var db = NewContext();
        var storeId = await SeedStore(db);
        var today = DateTimeOffset.UtcNow;
        db.DailyClosings.Add(DailyClosing.Create(storeId, today));
        await db.SaveChangesAsync();

        var handler = new CreateDailyClosingCommandHandler(db);
        var result = await handler.Handle(new CreateDailyClosingCommand(
            storeId, today, Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
    }
}
