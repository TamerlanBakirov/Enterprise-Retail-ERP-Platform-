using FluentAssertions;
using GeorgiaERP.Domain.Procurement;
using Xunit;

namespace GeorgiaERP.Tests.Domain;

public class PurchaseOrderTests
{
    private static PurchaseOrder NewOrder() =>
        PurchaseOrder.Create("PO-2026-0001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

    [Fact]
    public void Create_StartsInDraftStatus()
    {
        var po = NewOrder();

        po.PoNumber.Should().Be("PO-2026-0001");
        po.Status.Should().Be(PurchaseOrderStatus.Draft);
        po.Subtotal.Should().Be(0);
        po.Total.Should().Be(0);
    }

    [Fact]
    public void SetTotals_StoresSubtotalVatAndTotal()
    {
        var po = NewOrder();

        po.SetTotals(1000m, 180m, 1180m);

        po.Subtotal.Should().Be(1000m);
        po.VatTotal.Should().Be(180m);
        po.Total.Should().Be(1180m);
    }

    [Fact]
    public void Approve_TransitionsToApprovedAndStampsApprover()
    {
        var po = NewOrder();
        var approver = Guid.NewGuid();

        po.Approve(approver);

        po.Status.Should().Be(PurchaseOrderStatus.Approved);
        po.ApprovedBy.Should().Be(approver);
        po.ApprovedAt.Should().NotBeNull();
    }

    [Fact]
    public void FullLifecycle_DraftToReceived()
    {
        var po = NewOrder();

        po.Approve(Guid.NewGuid());
        po.Status.Should().Be(PurchaseOrderStatus.Approved);

        po.Send();
        po.Status.Should().Be(PurchaseOrderStatus.Sent);

        po.MarkPartiallyReceived();
        po.Status.Should().Be(PurchaseOrderStatus.PartiallyReceived);

        po.MarkReceived();
        po.Status.Should().Be(PurchaseOrderStatus.Received);
    }

    [Fact]
    public void Cancel_TransitionsToCancelled()
    {
        var po = NewOrder();

        po.Cancel();

        po.Status.Should().Be(PurchaseOrderStatus.Cancelled);
    }

    [Fact]
    public void SetExpectedDateAndNotes_StoresValues()
    {
        var po = NewOrder();
        var expected = DateTimeOffset.UtcNow.AddDays(7);

        po.SetExpectedDate(expected);
        po.SetNotes("Urgent restock");

        po.ExpectedDate.Should().Be(expected);
        po.Notes.Should().Be("Urgent restock");
    }
}
