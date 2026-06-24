using FluentAssertions;
using GeorgiaERP.Application.Compliance.Commands;
using GeorgiaERP.Application.Compliance.Queries;
using GeorgiaERP.Domain.POS;
using GeorgiaERP.Domain.Procurement;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GeorgiaERP.Tests.Application;

public class VatDeclarationHandlerTests
{
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"vat-{Guid.NewGuid()}")
            .Options);

    private static PosTransaction Sale(decimal vat, DateTimeOffset when)
    {
        var tx = PosTransaction.Create($"T-{Guid.NewGuid():N}", Guid.NewGuid(), Guid.NewGuid(),
            PosTransactionType.Sale, Guid.NewGuid());
        tx.SetTotals(100m, 0m, vat, 100m + vat);
        tx.Complete();
        SetCreatedAt(tx, when);
        return tx;
    }

    private static PosTransaction Return(decimal vat, DateTimeOffset when)
    {
        var tx = PosTransaction.Create($"R-{Guid.NewGuid():N}", Guid.NewGuid(), Guid.NewGuid(),
            PosTransactionType.Return, Guid.NewGuid());
        tx.SetTotals(100m, 0m, vat, 100m + vat);
        tx.Complete();
        SetCreatedAt(tx, when);
        return tx;
    }

    private static PurchaseOrder ReceivedPo(decimal vat, DateTimeOffset orderDate)
    {
        var po = PurchaseOrder.Create($"PO-{Guid.NewGuid():N}", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        po.SetTotals(200m, vat, 200m + vat);
        po.Approve(Guid.NewGuid());
        po.Send();
        po.MarkReceived();
        SetProperty(po, "OrderDate", orderDate);
        return po;
    }

    // CreatedAt / OrderDate are private-set; the period filters key off them, so
    // tests pin them via reflection to land inside the target month.
    private static void SetCreatedAt(object entity, DateTimeOffset value) =>
        SetProperty(entity, "CreatedAt", value);

    private static void SetProperty(object entity, string name, DateTimeOffset value)
    {
        var prop = entity.GetType().GetProperty(name)!;
        prop.SetValue(entity, value);
    }

    [Fact]
    public async Task Generate_ComputesOutputInputAndNetVat()
    {
        await using var db = NewContext();
        var inMonth = new DateTimeOffset(2026, 3, 10, 12, 0, 0, TimeSpan.Zero);
        db.PosTransactions.Add(Sale(180m, inMonth));
        db.PosTransactions.Add(Sale(20m, inMonth));
        db.PosTransactions.Add(Return(30m, inMonth));
        db.PurchaseOrders.Add(ReceivedPo(50m, inMonth));
        // Outside the period — must be ignored.
        db.PosTransactions.Add(Sale(999m, new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero)));
        await db.SaveChangesAsync();

        var result = await new GenerateVatDeclarationCommandHandler(db)
            .Handle(new GenerateVatDeclarationCommand(2026, 3), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalOutputVat.Should().Be(170m); // (180 + 20) - 30 returns
        result.Value.TotalInputVat.Should().Be(50m);
        result.Value.NetVat.Should().Be(120m);
        result.Value.Status.Should().Be("Draft");
    }

    [Fact]
    public async Task Generate_DuplicatePeriod_ReturnsConflict()
    {
        await using var db = NewContext();
        var handler = new GenerateVatDeclarationCommandHandler(db);
        await handler.Handle(new GenerateVatDeclarationCommand(2026, 3), CancellationToken.None);

        var second = await handler.Handle(new GenerateVatDeclarationCommand(2026, 3), CancellationToken.None);

        second.IsFailure.Should().BeTrue();
        second.ErrorCode.Should().Be("CONFLICT");
    }

    [Fact]
    public async Task Generate_NoActivity_YieldsZeroTotals()
    {
        await using var db = NewContext();

        var result = await new GenerateVatDeclarationCommandHandler(db)
            .Handle(new GenerateVatDeclarationCommand(2026, 3), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.NetVat.Should().Be(0m);
    }

    [Fact]
    public async Task Submit_DraftDeclaration_TransitionsToSubmitted()
    {
        await using var db = NewContext();
        var generated = await new GenerateVatDeclarationCommandHandler(db)
            .Handle(new GenerateVatDeclarationCommand(2026, 3), CancellationToken.None);

        var result = await new SubmitVatDeclarationCommandHandler(db)
            .Handle(new SubmitVatDeclarationCommand(generated.Value!.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("Submitted");
        result.Value.RsGeReference.Should().NotBeNullOrEmpty();
        result.Value.SubmittedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Submit_AlreadySubmitted_ReturnsConflict()
    {
        await using var db = NewContext();
        var generated = await new GenerateVatDeclarationCommandHandler(db)
            .Handle(new GenerateVatDeclarationCommand(2026, 3), CancellationToken.None);
        var submit = new SubmitVatDeclarationCommandHandler(db);
        await submit.Handle(new SubmitVatDeclarationCommand(generated.Value!.Id), CancellationToken.None);

        var again = await submit.Handle(new SubmitVatDeclarationCommand(generated.Value.Id), CancellationToken.None);

        again.IsFailure.Should().BeTrue();
        again.ErrorCode.Should().Be("CONFLICT");
    }

    [Fact]
    public async Task Submit_MissingDeclaration_ReturnsNotFound()
    {
        await using var db = NewContext();

        var result = await new SubmitVatDeclarationCommandHandler(db)
            .Handle(new SubmitVatDeclarationCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task GetById_ReturnsDeclaration()
    {
        await using var db = NewContext();
        var generated = await new GenerateVatDeclarationCommandHandler(db)
            .Handle(new GenerateVatDeclarationCommand(2026, 3), CancellationToken.None);

        var result = await new GetVatDeclarationByIdQueryHandler(db)
            .Handle(new GetVatDeclarationByIdQuery(generated.Value!.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(generated.Value.Id);
    }

    [Fact]
    public async Task GetList_ReturnsNewestPeriodFirst()
    {
        await using var db = NewContext();
        var handler = new GenerateVatDeclarationCommandHandler(db);
        await handler.Handle(new GenerateVatDeclarationCommand(2026, 1), CancellationToken.None);
        await handler.Handle(new GenerateVatDeclarationCommand(2026, 3), CancellationToken.None);

        var list = await new GetVatDeclarationsQueryHandler(db)
            .Handle(new GetVatDeclarationsQuery(1, 20), CancellationToken.None);

        list.Should().HaveCount(2);
        list[0].PeriodStart.Month.Should().Be(3);
    }
}
