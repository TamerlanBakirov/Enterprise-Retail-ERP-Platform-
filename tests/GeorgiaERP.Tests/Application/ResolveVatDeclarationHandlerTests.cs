using FluentAssertions;
using GeorgiaERP.Application.Compliance.Commands;
using GeorgiaERP.Domain.Compliance;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GeorgiaERP.Tests.Application;

public class ResolveVatDeclarationHandlerTests
{
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"vat-resolve-{Guid.NewGuid()}")
            .Options);

    private static async Task<VatDeclaration> SeedSubmitted(AppDbContext db)
    {
        var d = VatDeclaration.Create(
            new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            Guid.NewGuid());
        d.SetTotals(120m, 40m);
        d.Submit("VAT-202605", Guid.NewGuid());
        db.VatDeclarations.Add(d);
        await db.SaveChangesAsync();
        return d;
    }

    [Fact]
    public async Task Resolve_Accept_MarksAccepted()
    {
        await using var db = NewContext();
        var d = await SeedSubmitted(db);

        var result = await new ResolveVatDeclarationCommandHandler(db)
            .Handle(new ResolveVatDeclarationCommand(d.Id, Accepted: true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("Accepted");
    }

    [Fact]
    public async Task Resolve_Reject_MarksRejected()
    {
        await using var db = NewContext();
        var d = await SeedSubmitted(db);

        var result = await new ResolveVatDeclarationCommandHandler(db)
            .Handle(new ResolveVatDeclarationCommand(d.Id, Accepted: false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("Rejected");
    }

    [Fact]
    public async Task Resolve_DraftDeclaration_ReturnsConflict()
    {
        await using var db = NewContext();
        var d = VatDeclaration.Create(
            new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            Guid.NewGuid());
        d.SetTotals(10m, 0m);
        db.VatDeclarations.Add(d);
        await db.SaveChangesAsync();

        var result = await new ResolveVatDeclarationCommandHandler(db)
            .Handle(new ResolveVatDeclarationCommand(d.Id, Accepted: true), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("CONFLICT");
    }

    [Fact]
    public async Task Resolve_Unknown_ReturnsNotFound()
    {
        await using var db = NewContext();

        var result = await new ResolveVatDeclarationCommandHandler(db)
            .Handle(new ResolveVatDeclarationCommand(Guid.NewGuid(), Accepted: true), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("NOT_FOUND");
    }
}
