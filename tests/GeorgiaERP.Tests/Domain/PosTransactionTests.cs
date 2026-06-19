using FluentAssertions;
using GeorgiaERP.Domain.POS;
using Xunit;

namespace GeorgiaERP.Tests.Domain;

public class PosTransactionTests
{
    private static PosTransaction NewSale() =>
        PosTransaction.Create(
            transactionNumber: "TX-0001",
            sessionId: Guid.NewGuid(),
            storeId: Guid.NewGuid(),
            transactionType: PosTransactionType.Sale,
            createdBy: Guid.NewGuid());

    [Fact]
    public void Create_StartsPendingSale()
    {
        var tx = NewSale();

        tx.TransactionType.Should().Be(PosTransactionType.Sale);
        tx.TransactionNumber.Should().Be("TX-0001");
    }

    [Fact]
    public void SetTotals_StoresAllAmounts()
    {
        var tx = NewSale();

        tx.SetTotals(subtotal: 118m, discountTotal: 0m, vatTotal: 18m, total: 118m);

        tx.Subtotal.Should().Be(118m);
        tx.VatTotal.Should().Be(18m);
        tx.Total.Should().Be(118m);
    }

    [Fact]
    public void Complete_MarksCompletedAndStoresFiscalRef()
    {
        var tx = NewSale();

        tx.Complete("FISCAL-99");

        tx.Status.Should().Be(PosTransactionStatus.Completed);
        tx.FiscalReceiptId.Should().Be("FISCAL-99");
    }

    [Fact]
    public void Void_MarksVoidedWithReasonAndUser()
    {
        var tx = NewSale();
        tx.Complete();
        var voidedBy = Guid.NewGuid();

        tx.Void(voidedBy, "customer returned items");

        tx.Status.Should().Be(PosTransactionStatus.Voided);
        tx.VoidedBy.Should().Be(voidedBy);
        tx.VoidReason.Should().Be("customer returned items");
        tx.VoidedAt.Should().NotBeNull();
    }

    [Fact]
    public void Lines_CanBeAdded()
    {
        var tx = NewSale();
        var line = PosTransactionLine.Create(
            tx.Id, 1, Guid.NewGuid(), "Coca-Cola 0.5L", 2m, 3.50m);

        tx.Lines.Add(line);

        tx.Lines.Should().ContainSingle();
        tx.Lines.First().ProductName.Should().Be("Coca-Cola 0.5L");
    }
}
