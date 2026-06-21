using FluentAssertions;
using GeorgiaERP.Domain.Procurement;
using Xunit;

namespace GeorgiaERP.Tests.Domain;

public class ProcurementTests
{
    // === Supplier ===

    [Fact]
    public void CreateSupplier_SetsDefaultValues()
    {
        var supplier = Supplier.Create("SUP-001", "Caucasus Foods", "კავკასიის საკვები", "123456789");

        supplier.Code.Should().Be("SUP-001");
        supplier.Name.Should().Be("Caucasus Foods");
        supplier.NameKa.Should().Be("კავკასიის საკვები");
        supplier.Tin.Should().Be("123456789");
        supplier.IsActive.Should().BeTrue();
        supplier.IsVatPayer.Should().BeFalse();
        supplier.Rating.Should().BeNull();
        supplier.CreditLimit.Should().BeNull();
    }

    [Fact]
    public void Supplier_SetContactInfo_UpdatesFields()
    {
        var supplier = Supplier.Create("SUP-001", "Test Supplier");
        var before = supplier.UpdatedAt;

        supplier.SetContactInfo("John Doe", "+995 555 123456", "john@example.ge", "Tbilisi, Rustaveli Ave 1");

        supplier.ContactPerson.Should().Be("John Doe");
        supplier.Phone.Should().Be("+995 555 123456");
        supplier.Email.Should().Be("john@example.ge");
        supplier.Address.Should().Be("Tbilisi, Rustaveli Ave 1");
        supplier.UpdatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Supplier_SetPaymentTerms_UpdatesFields()
    {
        var supplier = Supplier.Create("SUP-001", "Test");

        supplier.SetPaymentTerms("Net 30", 50000m);

        supplier.PaymentTerms.Should().Be("Net 30");
        supplier.CreditLimit.Should().Be(50000m);
    }

    [Fact]
    public void Supplier_SetVatPayer_UpdatesFlag()
    {
        var supplier = Supplier.Create("SUP-001", "Test");

        supplier.SetVatPayer(true);

        supplier.IsVatPayer.Should().BeTrue();
    }

    [Fact]
    public void Supplier_SetRating_UpdatesRating()
    {
        var supplier = Supplier.Create("SUP-001", "Test");

        supplier.SetRating(5);

        supplier.Rating.Should().Be(5);
    }

    [Fact]
    public void Supplier_DeactivateAndActivate_TogglesStatus()
    {
        var supplier = Supplier.Create("SUP-001", "Test");

        supplier.Deactivate();
        supplier.IsActive.Should().BeFalse();

        supplier.Activate();
        supplier.IsActive.Should().BeTrue();
    }

    // === PurchaseOrder ===

    [Fact]
    public void CreatePurchaseOrder_SetsDefaultValues()
    {
        var supplierId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var po = PurchaseOrder.Create("PO-001", supplierId, warehouseId, userId);

        po.PoNumber.Should().Be("PO-001");
        po.SupplierId.Should().Be(supplierId);
        po.WarehouseId.Should().Be(warehouseId);
        po.CreatedBy.Should().Be(userId);
        po.Status.Should().Be(PurchaseOrderStatus.Draft);
        po.Subtotal.Should().Be(0);
        po.VatTotal.Should().Be(0);
        po.Total.Should().Be(0);
        po.ApprovedBy.Should().BeNull();
        po.ApprovedAt.Should().BeNull();
    }

    [Fact]
    public void PurchaseOrder_SetTotals_UpdatesAmounts()
    {
        var po = PurchaseOrder.Create("PO-001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        po.SetTotals(1000m, 180m, 1180m);

        po.Subtotal.Should().Be(1000m);
        po.VatTotal.Should().Be(180m);
        po.Total.Should().Be(1180m);
    }

    [Fact]
    public void PurchaseOrder_SetExpectedDate_UpdatesDate()
    {
        var po = PurchaseOrder.Create("PO-001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var expected = DateTimeOffset.UtcNow.AddDays(7);

        po.SetExpectedDate(expected);

        po.ExpectedDate.Should().Be(expected);
    }

    [Fact]
    public void PurchaseOrder_Approve_SetsApprovedStatus()
    {
        var po = PurchaseOrder.Create("PO-001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var approver = Guid.NewGuid();

        po.Approve(approver);

        po.Status.Should().Be(PurchaseOrderStatus.Approved);
        po.ApprovedBy.Should().Be(approver);
        po.ApprovedAt.Should().NotBeNull();
    }

    [Fact]
    public void PurchaseOrder_FullLifecycle_TransitionsCorrectly()
    {
        var po = PurchaseOrder.Create("PO-001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

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
    public void PurchaseOrder_Cancel_SetsCancelledStatus()
    {
        var po = PurchaseOrder.Create("PO-001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        po.Cancel();

        po.Status.Should().Be(PurchaseOrderStatus.Cancelled);
    }

    [Fact]
    public void PurchaseOrder_Approve_ThrowsWhenAlreadyApproved()
    {
        var po = PurchaseOrder.Create("PO-001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        po.Approve(Guid.NewGuid());

        var act = () => po.Approve(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot approve*");
    }

    [Fact]
    public void PurchaseOrder_Send_ThrowsWhenNotApproved()
    {
        var po = PurchaseOrder.Create("PO-001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        var act = () => po.Send();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot send*");
    }

    [Fact]
    public void PurchaseOrder_MarkReceived_ThrowsWhenNotSent()
    {
        var po = PurchaseOrder.Create("PO-001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        var act = () => po.MarkReceived();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void PurchaseOrder_Cancel_ThrowsWhenReceived()
    {
        var po = PurchaseOrder.Create("PO-001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        po.Approve(Guid.NewGuid());
        po.Send();
        po.MarkReceived();

        var act = () => po.Cancel();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot cancel*");
    }

    [Fact]
    public void PurchaseOrder_SetTotals_ThrowsOnNegativeValues()
    {
        var po = PurchaseOrder.Create("PO-001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        var act = () => po.SetTotals(-100m, 0m, 0m);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*negative*");
    }

    [Fact]
    public void PurchaseOrder_SetNotes_UpdatesNotes()
    {
        var po = PurchaseOrder.Create("PO-001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        po.SetNotes("Urgent delivery required");

        po.Notes.Should().Be("Urgent delivery required");
    }

    // === PurchaseOrderLine ===

    [Fact]
    public void CreatePurchaseOrderLine_SetsDefaultValues()
    {
        var poId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var line = PurchaseOrderLine.Create(poId, 1, productId, 100m, 25.50m);

        line.PurchaseOrderId.Should().Be(poId);
        line.LineNumber.Should().Be(1);
        line.ProductId.Should().Be(productId);
        line.OrderedQty.Should().Be(100m);
        line.UnitPrice.Should().Be(25.50m);
        line.ReceivedQty.Should().Be(0m);
        line.VariantId.Should().BeNull();
    }

    [Fact]
    public void PurchaseOrderLine_AddReceivedQty_AccumulatesQuantity()
    {
        var line = PurchaseOrderLine.Create(Guid.NewGuid(), 1, Guid.NewGuid(), 100m, 10m);

        line.AddReceivedQty(30m);
        line.AddReceivedQty(20m);

        line.ReceivedQty.Should().Be(50m);
        line.RemainingQty.Should().Be(50m);
    }

    [Fact]
    public void PurchaseOrderLine_RemainingQty_CalculatesCorrectly()
    {
        var line = PurchaseOrderLine.Create(Guid.NewGuid(), 1, Guid.NewGuid(), 100m, 10m);
        line.AddReceivedQty(100m);

        line.RemainingQty.Should().Be(0m);
    }

    [Fact]
    public void PurchaseOrderLine_SetVatAndTotal_UpdatesValues()
    {
        var line = PurchaseOrderLine.Create(Guid.NewGuid(), 1, Guid.NewGuid(), 10m, 100m);

        line.SetVat(180m);
        line.SetLineTotal(1180m);

        line.VatAmount.Should().Be(180m);
        line.LineTotal.Should().Be(1180m);
    }

    // === GoodsReceiptNote ===

    [Fact]
    public void CreateGoodsReceiptNote_SetsDefaultValues()
    {
        var poId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        var receivedBy = Guid.NewGuid();

        var grn = GoodsReceiptNote.Create("GRN-001", poId, warehouseId, supplierId, receivedBy);

        grn.GrnNumber.Should().Be("GRN-001");
        grn.PurchaseOrderId.Should().Be(poId);
        grn.WarehouseId.Should().Be(warehouseId);
        grn.SupplierId.Should().Be(supplierId);
        grn.ReceivedBy.Should().Be(receivedBy);
        grn.Status.Should().Be(GoodsReceiptStatus.Draft);
        grn.RsGeWaybillId.Should().BeNull();
    }

    [Fact]
    public void GoodsReceiptNote_Complete_SetsCompletedStatus()
    {
        var grn = GoodsReceiptNote.Create("GRN-001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        grn.Complete();

        grn.Status.Should().Be(GoodsReceiptStatus.Completed);
    }

    [Fact]
    public void GoodsReceiptNote_Cancel_SetsCancelledStatus()
    {
        var grn = GoodsReceiptNote.Create("GRN-001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        grn.Cancel();

        grn.Status.Should().Be(GoodsReceiptStatus.Cancelled);
    }

    [Fact]
    public void GoodsReceiptNote_LinkWaybill_SetsWaybillId()
    {
        var grn = GoodsReceiptNote.Create("GRN-001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var waybillId = Guid.NewGuid();

        grn.LinkWaybill(waybillId);

        grn.RsGeWaybillId.Should().Be(waybillId);
    }

    // === GoodsReceiptLine ===

    [Fact]
    public void CreateGoodsReceiptLine_SetsDefaultValues()
    {
        var grnId = Guid.NewGuid();
        var poLineId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var line = GoodsReceiptLine.Create(grnId, poLineId, productId, 50m, 25m);

        line.GrnId.Should().Be(grnId);
        line.PoLineId.Should().Be(poLineId);
        line.ProductId.Should().Be(productId);
        line.ReceivedQty.Should().Be(50m);
        line.AcceptedQty.Should().Be(50m);
        line.RejectedQty.Should().Be(0m);
        line.UnitCost.Should().Be(25m);
    }

    [Fact]
    public void GoodsReceiptLine_SetQualityResult_UpdatesQuantities()
    {
        var line = GoodsReceiptLine.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m, 10m);

        line.SetQualityResult(95m, 5m);

        line.AcceptedQty.Should().Be(95m);
        line.RejectedQty.Should().Be(5m);
    }

    [Fact]
    public void GoodsReceiptLine_SetBatch_UpdatesBatchInfo()
    {
        var line = GoodsReceiptLine.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m, 10m);
        var expiry = DateTimeOffset.UtcNow.AddMonths(6);

        line.SetBatch("BATCH-2026-001", expiry);

        line.BatchNumber.Should().Be("BATCH-2026-001");
        line.ExpiryDate.Should().Be(expiry);
    }
}
