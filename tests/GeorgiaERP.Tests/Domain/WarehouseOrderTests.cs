using FluentAssertions;
using GeorgiaERP.Domain.Warehouse;
using Xunit;

namespace GeorgiaERP.Tests.Domain;

public class WarehouseOrderTests
{
    // === ReceivingOrder ===

    [Fact]
    public void ReceivingOrder_Create_SetsExpectedDefaults()
    {
        var whId = Guid.NewGuid();
        var order = ReceivingOrder.Create("RCV-001", whId, ReceivingOrderSource.PurchaseOrder);

        order.ReceivingNumber.Should().Be("RCV-001");
        order.WarehouseId.Should().Be(whId);
        order.Status.Should().Be(ReceivingOrderStatus.Expected);
        order.Source.Should().Be(ReceivingOrderSource.PurchaseOrder);
        order.SourceOrderId.Should().BeNull();
        order.SupplierId.Should().BeNull();
    }

    [Fact]
    public void ReceivingOrder_Create_WithOptionalParams()
    {
        var sourceId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        var order = ReceivingOrder.Create("RCV-002", Guid.NewGuid(), ReceivingOrderSource.TransferOrder, sourceId, supplierId);

        order.SourceOrderId.Should().Be(sourceId);
        order.SupplierId.Should().Be(supplierId);
    }

    [Fact]
    public void ReceivingOrder_StartReceiving_TransitionsToInProgress()
    {
        var order = ReceivingOrder.Create("RCV-003", Guid.NewGuid(), ReceivingOrderSource.Manual);

        order.StartReceiving();

        order.Status.Should().Be(ReceivingOrderStatus.InProgress);
    }

    [Fact]
    public void ReceivingOrder_StartReceiving_WhenNotExpected_Throws()
    {
        var order = ReceivingOrder.Create("RCV-004", Guid.NewGuid(), ReceivingOrderSource.Manual);
        order.StartReceiving();

        var act = () => order.StartReceiving();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ReceivingOrder_Complete_SetsReceivedByAndTimestamp()
    {
        var userId = Guid.NewGuid();
        var order = ReceivingOrder.Create("RCV-005", Guid.NewGuid(), ReceivingOrderSource.Manual);
        order.StartReceiving();

        order.Complete(userId);

        order.Status.Should().Be(ReceivingOrderStatus.Completed);
        order.ReceivedBy.Should().Be(userId);
        order.ReceivedAt.Should().NotBeNull();
    }

    [Fact]
    public void ReceivingOrder_Complete_WithEmptyGuid_Throws()
    {
        var order = ReceivingOrder.Create("RCV-006", Guid.NewGuid(), ReceivingOrderSource.Manual);
        order.StartReceiving();

        var act = () => order.Complete(Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ReceivingOrder_Complete_WhenNotInProgress_Throws()
    {
        var order = ReceivingOrder.Create("RCV-007", Guid.NewGuid(), ReceivingOrderSource.Manual);

        var act = () => order.Complete(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ReceivingOrder_Cancel_Works()
    {
        var order = ReceivingOrder.Create("RCV-008", Guid.NewGuid(), ReceivingOrderSource.Manual);

        order.Cancel();

        order.Status.Should().Be(ReceivingOrderStatus.Cancelled);
    }

    [Fact]
    public void ReceivingOrder_Cancel_WhenCompleted_Throws()
    {
        var order = ReceivingOrder.Create("RCV-009", Guid.NewGuid(), ReceivingOrderSource.Manual);
        order.StartReceiving();
        order.Complete(Guid.NewGuid());

        var act = () => order.Cancel();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ReceivingOrder_SetExpectedDate_UpdatesTimestamp()
    {
        var order = ReceivingOrder.Create("RCV-010", Guid.NewGuid(), ReceivingOrderSource.Manual);
        var beforeUpdate = order.UpdatedAt;

        order.SetExpectedDate(DateTimeOffset.UtcNow.AddDays(3));

        order.ExpectedDate.Should().NotBeNull();
        order.UpdatedAt.Should().BeOnOrAfter(beforeUpdate);
    }

    [Fact]
    public void ReceivingOrder_SetLocation_UpdatesField()
    {
        var order = ReceivingOrder.Create("RCV-011", Guid.NewGuid(), ReceivingOrderSource.Manual);
        var locId = Guid.NewGuid();

        order.SetLocation(locId);

        order.LocationId.Should().Be(locId);
    }

    [Fact]
    public void ReceivingOrder_SetNotes_UpdatesField()
    {
        var order = ReceivingOrder.Create("RCV-012", Guid.NewGuid(), ReceivingOrderSource.Manual);

        order.SetNotes("Test notes");

        order.Notes.Should().Be("Test notes");
    }

    // === ReceivingOrderLine ===

    [Fact]
    public void ReceivingOrderLine_Create_SetsDefaults()
    {
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var line = ReceivingOrderLine.Create(orderId, productId, 100m);

        line.ReceivingOrderId.Should().Be(orderId);
        line.ProductId.Should().Be(productId);
        line.ExpectedQty.Should().Be(100m);
        line.ReceivedQty.Should().Be(0);
        line.VariantId.Should().BeNull();
    }

    [Fact]
    public void ReceivingOrderLine_Receive_SetsQuantities()
    {
        var line = ReceivingOrderLine.Create(Guid.NewGuid(), Guid.NewGuid(), 100m);

        line.Receive(95m, 5m);

        line.ReceivedQty.Should().Be(95m);
        line.DamagedQty.Should().Be(5m);
    }

    [Fact]
    public void ReceivingOrderLine_SetBatch_SetsTrackingInfo()
    {
        var line = ReceivingOrderLine.Create(Guid.NewGuid(), Guid.NewGuid(), 50m);

        line.SetBatch("BATCH-001", "SN-001", new DateOnly(2027, 1, 1));

        line.BatchNumber.Should().Be("BATCH-001");
        line.SerialNumber.Should().Be("SN-001");
        line.ExpiryDate.Should().Be(new DateOnly(2027, 1, 1));
    }

    // === ShippingOrder ===

    [Fact]
    public void ShippingOrder_Create_SetsDefaults()
    {
        var whId = Guid.NewGuid();
        var order = ShippingOrder.Create("SHP-001", whId, ShippingOrderType.SalesOrder);

        order.ShippingNumber.Should().Be("SHP-001");
        order.WarehouseId.Should().Be(whId);
        order.Status.Should().Be(ShippingOrderStatus.Draft);
        order.OrderType.Should().Be(ShippingOrderType.SalesOrder);
    }

    [Fact]
    public void ShippingOrder_FullWorkflow_DraftToDelivered()
    {
        var order = ShippingOrder.Create("SHP-002", Guid.NewGuid(), ShippingOrderType.Manual);

        order.StartPicking();
        order.Status.Should().Be(ShippingOrderStatus.Picking);

        order.MarkPacked();
        order.Status.Should().Be(ShippingOrderStatus.Packed);

        order.Ship(Guid.NewGuid(), "TRACK-123");
        order.Status.Should().Be(ShippingOrderStatus.Shipped);
        order.TrackingNumber.Should().Be("TRACK-123");

        order.MarkDelivered();
        order.Status.Should().Be(ShippingOrderStatus.Delivered);
        order.DeliveredAt.Should().NotBeNull();
    }

    [Fact]
    public void ShippingOrder_MarkReadyToShip_FromPacked()
    {
        var order = ShippingOrder.Create("SHP-003", Guid.NewGuid(), ShippingOrderType.Manual);
        order.StartPicking();
        order.MarkPacked();

        order.MarkReadyToShip();

        order.Status.Should().Be(ShippingOrderStatus.ReadyToShip);
    }

    [Fact]
    public void ShippingOrder_Ship_FromReadyToShip()
    {
        var order = ShippingOrder.Create("SHP-004", Guid.NewGuid(), ShippingOrderType.Manual);
        order.StartPicking();
        order.MarkPacked();
        order.MarkReadyToShip();

        order.Ship(Guid.NewGuid());

        order.Status.Should().Be(ShippingOrderStatus.Shipped);
    }

    [Fact]
    public void ShippingOrder_StartPicking_WhenNotDraft_Throws()
    {
        var order = ShippingOrder.Create("SHP-005", Guid.NewGuid(), ShippingOrderType.Manual);
        order.StartPicking();

        var act = () => order.StartPicking();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ShippingOrder_MarkPacked_WhenNotPicking_Throws()
    {
        var order = ShippingOrder.Create("SHP-006", Guid.NewGuid(), ShippingOrderType.Manual);

        var act = () => order.MarkPacked();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ShippingOrder_Ship_WithEmptyGuid_Throws()
    {
        var order = ShippingOrder.Create("SHP-007", Guid.NewGuid(), ShippingOrderType.Manual);
        order.StartPicking();
        order.MarkPacked();

        var act = () => order.Ship(Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ShippingOrder_Cancel_WhenShipped_Throws()
    {
        var order = ShippingOrder.Create("SHP-008", Guid.NewGuid(), ShippingOrderType.Manual);
        order.StartPicking();
        order.MarkPacked();
        order.Ship(Guid.NewGuid());

        var act = () => order.Cancel();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ShippingOrder_Cancel_WhenDelivered_Throws()
    {
        var order = ShippingOrder.Create("SHP-009", Guid.NewGuid(), ShippingOrderType.Manual);
        order.StartPicking();
        order.MarkPacked();
        order.Ship(Guid.NewGuid());
        order.MarkDelivered();

        var act = () => order.Cancel();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ShippingOrder_Cancel_WhenDraft_Works()
    {
        var order = ShippingOrder.Create("SHP-010", Guid.NewGuid(), ShippingOrderType.Manual);

        order.Cancel();

        order.Status.Should().Be(ShippingOrderStatus.Cancelled);
    }

    [Fact]
    public void ShippingOrder_SetShippingDetails_UpdatesFields()
    {
        var order = ShippingOrder.Create("SHP-011", Guid.NewGuid(), ShippingOrderType.SalesOrder);
        var shipDate = DateTimeOffset.UtcNow.AddDays(2);

        order.SetShippingDetails("123 Main St", "DHL", shipDate);

        order.ShippingAddress.Should().Be("123 Main St");
        order.Carrier.Should().Be("DHL");
        order.ExpectedShipDate.Should().Be(shipDate);
    }

    [Fact]
    public void ShippingOrder_LinkWaybill_SetsId()
    {
        var order = ShippingOrder.Create("SHP-012", Guid.NewGuid(), ShippingOrderType.SalesOrder);
        var waybillId = Guid.NewGuid();

        order.LinkWaybill(waybillId);

        order.RsGeWaybillId.Should().Be(waybillId);
    }

    // === ShippingOrderLine ===

    [Fact]
    public void ShippingOrderLine_Create_SetsDefaults()
    {
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var line = ShippingOrderLine.Create(orderId, productId, 50m);

        line.ShippingOrderId.Should().Be(orderId);
        line.ProductId.Should().Be(productId);
        line.OrderedQty.Should().Be(50m);
        line.PickedQty.Should().Be(0);
        line.PackedQty.Should().Be(0);
        line.ShippedQty.Should().Be(0);
    }

    [Fact]
    public void ShippingOrderLine_Pick_SetsQuantityAndLocation()
    {
        var line = ShippingOrderLine.Create(Guid.NewGuid(), Guid.NewGuid(), 50m);
        var locationId = Guid.NewGuid();

        line.Pick(45m, locationId);

        line.PickedQty.Should().Be(45m);
        line.PickLocationId.Should().Be(locationId);
    }

    [Fact]
    public void ShippingOrderLine_Pack_SetsQuantity()
    {
        var line = ShippingOrderLine.Create(Guid.NewGuid(), Guid.NewGuid(), 50m);

        line.Pack(50m);

        line.PackedQty.Should().Be(50m);
    }

    // === WarehouseLocation ===

    [Fact]
    public void WarehouseLocation_Create_SetsDefaults()
    {
        var whId = Guid.NewGuid();
        var loc = WarehouseLocation.Create(whId, "A-01", "Zone A", LocationType.Zone, nameKa: "ზონა ა");

        loc.WarehouseId.Should().Be(whId);
        loc.Code.Should().Be("A-01");
        loc.Name.Should().Be("Zone A");
        loc.NameKa.Should().Be("ზონა ა");
        loc.LocationType.Should().Be(LocationType.Zone);
        loc.IsActive.Should().BeTrue();
    }

    [Fact]
    public void WarehouseLocation_Deactivate_SetsInactive()
    {
        var loc = WarehouseLocation.Create(Guid.NewGuid(), "A-02", "Zone B", LocationType.Aisle);

        loc.Deactivate();

        loc.IsActive.Should().BeFalse();
    }

    [Fact]
    public void WarehouseLocation_Activate_SetsActive()
    {
        var loc = WarehouseLocation.Create(Guid.NewGuid(), "A-03", "Zone C", LocationType.Rack);
        loc.Deactivate();

        loc.Activate();

        loc.IsActive.Should().BeTrue();
    }

    [Fact]
    public void WarehouseLocation_Update_ChangesFields()
    {
        var loc = WarehouseLocation.Create(Guid.NewGuid(), "A-04", "Zone D", LocationType.Shelf);

        loc.Update("Updated Zone", "განახლებული ზონა", 5, 1000, "Some notes");

        loc.Name.Should().Be("Updated Zone");
        loc.NameKa.Should().Be("განახლებული ზონა");
        loc.SortOrder.Should().Be(5);
        loc.MaxCapacity.Should().Be(1000);
        loc.Notes.Should().Be("Some notes");
    }

    // === DailyClosing ===

    [Fact]
    public void DailyClosing_Create_SetsDefaults()
    {
        var storeId = Guid.NewGuid();
        var closingDate = DateTimeOffset.UtcNow;
        var closing = GeorgiaERP.Domain.POS.DailyClosing.Create(storeId, closingDate);

        closing.StoreId.Should().Be(storeId);
        closing.ClosingDate.Should().Be(closingDate);
        closing.Status.Should().Be(GeorgiaERP.Domain.POS.DailyClosingStatus.Draft);
        closing.TotalSales.Should().Be(0);
        closing.TransactionCount.Should().Be(0);
    }
}
