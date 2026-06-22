using FluentAssertions;
using GeorgiaERP.Domain.Organization;
using GeorgiaERP.Domain.Warehouse;
using Xunit;

namespace GeorgiaERP.Tests.Domain;

public class WarehouseTests
{
    // === Warehouse Entity Tests ===

    [Fact]
    public void Warehouse_Create_SetsProperties()
    {
        var wh = Warehouse.Create("WH-001", "Main Warehouse", WarehouseType.Central, "მთავარი საწყობი");

        wh.Code.Should().Be("WH-001");
        wh.Name.Should().Be("Main Warehouse");
        wh.NameKa.Should().Be("მთავარი საწყობი");
        wh.WarehouseType.Should().Be(WarehouseType.Central);
        wh.IsActive.Should().BeTrue();
        wh.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Warehouse_Update_ChangesFields()
    {
        var wh = Warehouse.Create("WH-001", "Old Name", WarehouseType.Regional);

        wh.Update("New Name", "ახალი სახელი", "123 Street", "Tbilisi", "Tbilisi Region");

        wh.Name.Should().Be("New Name");
        wh.NameKa.Should().Be("ახალი სახელი");
        wh.Address.Should().Be("123 Street");
        wh.City.Should().Be("Tbilisi");
        wh.Region.Should().Be("Tbilisi Region");
    }

    [Fact]
    public void Warehouse_LinkToStore_SetsStoreId()
    {
        var wh = Warehouse.Create("WH-001", "Store Warehouse", WarehouseType.Store);
        var storeId = Guid.NewGuid();

        wh.LinkToStore(storeId);

        wh.LinkedStoreId.Should().Be(storeId);
    }

    [Fact]
    public void Warehouse_Deactivate_SetsInactive()
    {
        var wh = Warehouse.Create("WH-001", "Test", WarehouseType.Central);

        wh.Deactivate();

        wh.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Warehouse_Activate_SetsActive()
    {
        var wh = Warehouse.Create("WH-001", "Test", WarehouseType.Central);
        wh.Deactivate();

        wh.Activate();

        wh.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData(WarehouseType.Central)]
    [InlineData(WarehouseType.Regional)]
    [InlineData(WarehouseType.Store)]
    public void Warehouse_AllTypes_AreSupported(WarehouseType type)
    {
        var wh = Warehouse.Create("WH-001", "Test", type);
        wh.WarehouseType.Should().Be(type);
    }

    // === WarehouseLocation Tests ===

    [Fact]
    public void WarehouseLocation_Create_SetsProperties()
    {
        var warehouseId = Guid.NewGuid();

        var loc = WarehouseLocation.Create(warehouseId, "A-01", "Zone A", LocationType.Zone, nameKa: "ზონა A");

        loc.WarehouseId.Should().Be(warehouseId);
        loc.Code.Should().Be("A-01");
        loc.Name.Should().Be("Zone A");
        loc.NameKa.Should().Be("ზონა A");
        loc.LocationType.Should().Be(LocationType.Zone);
        loc.IsActive.Should().BeTrue();
        loc.ParentLocationId.Should().BeNull();
    }

    [Fact]
    public void WarehouseLocation_Create_WithParent()
    {
        var parentId = Guid.NewGuid();
        var loc = WarehouseLocation.Create(Guid.NewGuid(), "R-01", "Rack 1", LocationType.Rack, parentId);

        loc.ParentLocationId.Should().Be(parentId);
    }

    [Theory]
    [InlineData(LocationType.Zone)]
    [InlineData(LocationType.Aisle)]
    [InlineData(LocationType.Rack)]
    [InlineData(LocationType.Shelf)]
    [InlineData(LocationType.Bin)]
    public void WarehouseLocation_AllTypes_AreSupported(LocationType type)
    {
        var loc = WarehouseLocation.Create(Guid.NewGuid(), "L-01", "Test", type);
        loc.LocationType.Should().Be(type);
    }

    [Fact]
    public void WarehouseLocation_Update_ChangesFields()
    {
        var loc = WarehouseLocation.Create(Guid.NewGuid(), "A-01", "Zone A", LocationType.Zone);

        loc.Update("Zone B", "ზონა B", 5, 1000, "High capacity zone");

        loc.Name.Should().Be("Zone B");
        loc.NameKa.Should().Be("ზონა B");
        loc.SortOrder.Should().Be(5);
        loc.MaxCapacity.Should().Be(1000);
        loc.Notes.Should().Be("High capacity zone");
    }

    [Fact]
    public void WarehouseLocation_Deactivate_SetsInactive()
    {
        var loc = WarehouseLocation.Create(Guid.NewGuid(), "A-01", "Zone A", LocationType.Zone);

        loc.Deactivate();

        loc.IsActive.Should().BeFalse();
    }

    [Fact]
    public void WarehouseLocation_Activate_SetsActive()
    {
        var loc = WarehouseLocation.Create(Guid.NewGuid(), "A-01", "Zone A", LocationType.Zone);
        loc.Deactivate();

        loc.Activate();

        loc.IsActive.Should().BeTrue();
    }

    // === ReceivingOrder Tests ===

    [Fact]
    public void ReceivingOrder_Create_StartsExpected()
    {
        var warehouseId = Guid.NewGuid();

        var order = ReceivingOrder.Create("RCV-001", warehouseId, ReceivingOrderSource.PurchaseOrder);

        order.ReceivingNumber.Should().Be("RCV-001");
        order.WarehouseId.Should().Be(warehouseId);
        order.Status.Should().Be(ReceivingOrderStatus.Expected);
        order.Source.Should().Be(ReceivingOrderSource.PurchaseOrder);
        order.ReceivedAt.Should().BeNull();
        order.ReceivedBy.Should().BeNull();
    }

    [Fact]
    public void ReceivingOrder_Create_WithSourceOrder()
    {
        var sourceId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();

        var order = ReceivingOrder.Create("RCV-002", Guid.NewGuid(), ReceivingOrderSource.PurchaseOrder, sourceId, supplierId);

        order.SourceOrderId.Should().Be(sourceId);
        order.SupplierId.Should().Be(supplierId);
    }

    [Theory]
    [InlineData(ReceivingOrderSource.PurchaseOrder)]
    [InlineData(ReceivingOrderSource.TransferOrder)]
    [InlineData(ReceivingOrderSource.Return)]
    [InlineData(ReceivingOrderSource.Manual)]
    public void ReceivingOrder_AllSources_AreSupported(ReceivingOrderSource source)
    {
        var order = ReceivingOrder.Create("RCV-001", Guid.NewGuid(), source);
        order.Source.Should().Be(source);
    }

    [Fact]
    public void ReceivingOrder_SetExpectedDate()
    {
        var order = ReceivingOrder.Create("RCV-001", Guid.NewGuid(), ReceivingOrderSource.Manual);
        var date = DateTimeOffset.UtcNow.AddDays(3);

        order.SetExpectedDate(date);

        order.ExpectedDate.Should().Be(date);
    }

    [Fact]
    public void ReceivingOrder_SetLocation()
    {
        var order = ReceivingOrder.Create("RCV-001", Guid.NewGuid(), ReceivingOrderSource.Manual);
        var locationId = Guid.NewGuid();

        order.SetLocation(locationId);

        order.LocationId.Should().Be(locationId);
    }

    [Fact]
    public void ReceivingOrder_SetNotes()
    {
        var order = ReceivingOrder.Create("RCV-001", Guid.NewGuid(), ReceivingOrderSource.Manual);

        order.SetNotes("Urgent delivery");

        order.Notes.Should().Be("Urgent delivery");
    }

    [Fact]
    public void ReceivingOrder_StartReceiving_TransitionsToInProgress()
    {
        var order = ReceivingOrder.Create("RCV-001", Guid.NewGuid(), ReceivingOrderSource.PurchaseOrder);

        order.StartReceiving();

        order.Status.Should().Be(ReceivingOrderStatus.InProgress);
    }

    [Fact]
    public void ReceivingOrder_Complete_SetsCompletedStatus()
    {
        var order = ReceivingOrder.Create("RCV-001", Guid.NewGuid(), ReceivingOrderSource.PurchaseOrder);
        order.StartReceiving();
        var receivedBy = Guid.NewGuid();

        order.Complete(receivedBy);

        order.Status.Should().Be(ReceivingOrderStatus.Completed);
        order.ReceivedBy.Should().Be(receivedBy);
        order.ReceivedAt.Should().NotBeNull();
    }

    [Fact]
    public void ReceivingOrder_Cancel_SetsCancelledStatus()
    {
        var order = ReceivingOrder.Create("RCV-001", Guid.NewGuid(), ReceivingOrderSource.PurchaseOrder);

        order.Cancel();

        order.Status.Should().Be(ReceivingOrderStatus.Cancelled);
    }

    [Fact]
    public void ReceivingOrder_StartReceiving_WhenNotExpected_Throws()
    {
        var order = ReceivingOrder.Create("RCV-001", Guid.NewGuid(), ReceivingOrderSource.PurchaseOrder);
        order.StartReceiving();

        var act = () => order.StartReceiving();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ReceivingOrder_Complete_WhenNotInProgress_Throws()
    {
        var order = ReceivingOrder.Create("RCV-001", Guid.NewGuid(), ReceivingOrderSource.PurchaseOrder);

        var act = () => order.Complete(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ReceivingOrder_Complete_WithEmptyGuid_Throws()
    {
        var order = ReceivingOrder.Create("RCV-001", Guid.NewGuid(), ReceivingOrderSource.PurchaseOrder);
        order.StartReceiving();

        var act = () => order.Complete(Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ReceivingOrder_Cancel_WhenCompleted_Throws()
    {
        var order = ReceivingOrder.Create("RCV-001", Guid.NewGuid(), ReceivingOrderSource.PurchaseOrder);
        order.StartReceiving();
        order.Complete(Guid.NewGuid());

        var act = () => order.Cancel();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ReceivingOrder_FullLifecycle()
    {
        var order = ReceivingOrder.Create("RCV-001", Guid.NewGuid(), ReceivingOrderSource.PurchaseOrder);

        order.Status.Should().Be(ReceivingOrderStatus.Expected);

        order.StartReceiving();
        order.Status.Should().Be(ReceivingOrderStatus.InProgress);

        order.Complete(Guid.NewGuid());
        order.Status.Should().Be(ReceivingOrderStatus.Completed);
    }

    // === ReceivingOrderLine Tests ===

    [Fact]
    public void ReceivingOrderLine_Create_SetsProperties()
    {
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var line = ReceivingOrderLine.Create(orderId, productId, 100m);

        line.ReceivingOrderId.Should().Be(orderId);
        line.ProductId.Should().Be(productId);
        line.ExpectedQty.Should().Be(100m);
        line.ReceivedQty.Should().Be(0);
    }

    [Fact]
    public void ReceivingOrderLine_Create_WithVariant()
    {
        var variantId = Guid.NewGuid();
        var line = ReceivingOrderLine.Create(Guid.NewGuid(), Guid.NewGuid(), 50m, variantId);

        line.VariantId.Should().Be(variantId);
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
    public void ReceivingOrderLine_SetBatch_SetsTracking()
    {
        var line = ReceivingOrderLine.Create(Guid.NewGuid(), Guid.NewGuid(), 50m);
        var expiry = new DateOnly(2027, 12, 31);

        line.SetBatch("BATCH-001", "SN-12345", expiry);

        line.BatchNumber.Should().Be("BATCH-001");
        line.SerialNumber.Should().Be("SN-12345");
        line.ExpiryDate.Should().Be(expiry);
    }

    [Fact]
    public void ReceivingOrderLine_SetLocation()
    {
        var line = ReceivingOrderLine.Create(Guid.NewGuid(), Guid.NewGuid(), 50m);
        var locationId = Guid.NewGuid();

        line.SetLocation(locationId);

        line.LocationId.Should().Be(locationId);
    }

    // === ShippingOrder Tests ===

    [Fact]
    public void ShippingOrder_Create_StartsDraft()
    {
        var warehouseId = Guid.NewGuid();

        var order = ShippingOrder.Create("SHP-001", warehouseId, ShippingOrderType.SalesOrder);

        order.ShippingNumber.Should().Be("SHP-001");
        order.WarehouseId.Should().Be(warehouseId);
        order.Status.Should().Be(ShippingOrderStatus.Draft);
        order.OrderType.Should().Be(ShippingOrderType.SalesOrder);
        order.ShippedAt.Should().BeNull();
    }

    [Theory]
    [InlineData(ShippingOrderType.SalesOrder)]
    [InlineData(ShippingOrderType.TransferOrder)]
    [InlineData(ShippingOrderType.Return)]
    [InlineData(ShippingOrderType.Manual)]
    public void ShippingOrder_AllTypes_AreSupported(ShippingOrderType type)
    {
        var order = ShippingOrder.Create("SHP-001", Guid.NewGuid(), type);
        order.OrderType.Should().Be(type);
    }

    [Fact]
    public void ShippingOrder_SetShippingDetails()
    {
        var order = ShippingOrder.Create("SHP-001", Guid.NewGuid(), ShippingOrderType.SalesOrder);
        var date = DateTimeOffset.UtcNow.AddDays(2);

        order.SetShippingDetails("123 Main St", "DHL", date);

        order.ShippingAddress.Should().Be("123 Main St");
        order.Carrier.Should().Be("DHL");
        order.ExpectedShipDate.Should().Be(date);
    }

    [Fact]
    public void ShippingOrder_SetDestWarehouse()
    {
        var order = ShippingOrder.Create("SHP-001", Guid.NewGuid(), ShippingOrderType.TransferOrder);
        var destId = Guid.NewGuid();

        order.SetDestWarehouse(destId);

        order.DestWarehouseId.Should().Be(destId);
    }

    [Fact]
    public void ShippingOrder_StartPicking()
    {
        var order = ShippingOrder.Create("SHP-001", Guid.NewGuid(), ShippingOrderType.SalesOrder);

        order.StartPicking();

        order.Status.Should().Be(ShippingOrderStatus.Picking);
    }

    [Fact]
    public void ShippingOrder_MarkPacked()
    {
        var order = ShippingOrder.Create("SHP-001", Guid.NewGuid(), ShippingOrderType.SalesOrder);
        order.StartPicking();

        order.MarkPacked();

        order.Status.Should().Be(ShippingOrderStatus.Packed);
    }

    [Fact]
    public void ShippingOrder_Ship_SetsShippedStatus()
    {
        var order = ShippingOrder.Create("SHP-001", Guid.NewGuid(), ShippingOrderType.SalesOrder);
        order.StartPicking();
        order.MarkPacked();
        var shippedBy = Guid.NewGuid();

        order.Ship(shippedBy, "TRACK-12345");

        order.Status.Should().Be(ShippingOrderStatus.Shipped);
        order.ShippedBy.Should().Be(shippedBy);
        order.TrackingNumber.Should().Be("TRACK-12345");
        order.ShippedAt.Should().NotBeNull();
    }

    [Fact]
    public void ShippingOrder_MarkDelivered()
    {
        var order = ShippingOrder.Create("SHP-001", Guid.NewGuid(), ShippingOrderType.SalesOrder);
        order.StartPicking();
        order.MarkPacked();
        order.Ship(Guid.NewGuid());

        order.MarkDelivered();

        order.Status.Should().Be(ShippingOrderStatus.Delivered);
        order.DeliveredAt.Should().NotBeNull();
    }

    [Fact]
    public void ShippingOrder_Cancel()
    {
        var order = ShippingOrder.Create("SHP-001", Guid.NewGuid(), ShippingOrderType.SalesOrder);

        order.Cancel();

        order.Status.Should().Be(ShippingOrderStatus.Cancelled);
    }

    [Fact]
    public void ShippingOrder_StartPicking_WhenNotDraft_Throws()
    {
        var order = ShippingOrder.Create("SHP-001", Guid.NewGuid(), ShippingOrderType.SalesOrder);
        order.StartPicking();

        var act = () => order.StartPicking();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ShippingOrder_MarkPacked_WhenNotPicking_Throws()
    {
        var order = ShippingOrder.Create("SHP-001", Guid.NewGuid(), ShippingOrderType.SalesOrder);

        var act = () => order.MarkPacked();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ShippingOrder_Ship_WhenNotPacked_Throws()
    {
        var order = ShippingOrder.Create("SHP-001", Guid.NewGuid(), ShippingOrderType.SalesOrder);
        order.StartPicking();

        var act = () => order.Ship(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ShippingOrder_Ship_WithEmptyGuid_Throws()
    {
        var order = ShippingOrder.Create("SHP-001", Guid.NewGuid(), ShippingOrderType.SalesOrder);
        order.StartPicking();
        order.MarkPacked();

        var act = () => order.Ship(Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ShippingOrder_Cancel_WhenShipped_Throws()
    {
        var order = ShippingOrder.Create("SHP-001", Guid.NewGuid(), ShippingOrderType.SalesOrder);
        order.StartPicking();
        order.MarkPacked();
        order.Ship(Guid.NewGuid());

        var act = () => order.Cancel();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ShippingOrder_MarkDelivered_WhenNotShipped_Throws()
    {
        var order = ShippingOrder.Create("SHP-001", Guid.NewGuid(), ShippingOrderType.SalesOrder);

        var act = () => order.MarkDelivered();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ShippingOrder_LinkWaybill()
    {
        var order = ShippingOrder.Create("SHP-001", Guid.NewGuid(), ShippingOrderType.SalesOrder);
        var waybillId = Guid.NewGuid();

        order.LinkWaybill(waybillId);

        order.RsGeWaybillId.Should().Be(waybillId);
    }

    [Fact]
    public void ShippingOrder_FullLifecycle()
    {
        var order = ShippingOrder.Create("SHP-001", Guid.NewGuid(), ShippingOrderType.SalesOrder);

        order.Status.Should().Be(ShippingOrderStatus.Draft);

        order.StartPicking();
        order.Status.Should().Be(ShippingOrderStatus.Picking);

        order.MarkPacked();
        order.Status.Should().Be(ShippingOrderStatus.Packed);

        order.Ship(Guid.NewGuid(), "TRACK-001");
        order.Status.Should().Be(ShippingOrderStatus.Shipped);

        order.MarkDelivered();
        order.Status.Should().Be(ShippingOrderStatus.Delivered);
    }

    // === ShippingOrderLine Tests ===

    [Fact]
    public void ShippingOrderLine_Create_SetsProperties()
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

        line.Pick(50m, locationId);

        line.PickedQty.Should().Be(50m);
        line.PickLocationId.Should().Be(locationId);
    }

    [Fact]
    public void ShippingOrderLine_Pack_SetsPackedQty()
    {
        var line = ShippingOrderLine.Create(Guid.NewGuid(), Guid.NewGuid(), 50m);
        line.Pick(50m);

        line.Pack(50m);

        line.PackedQty.Should().Be(50m);
    }

    [Fact]
    public void ShippingOrderLine_SetShippedQty()
    {
        var line = ShippingOrderLine.Create(Guid.NewGuid(), Guid.NewGuid(), 50m);
        line.Pick(50m);
        line.Pack(50m);

        line.SetShippedQty(50m);

        line.ShippedQty.Should().Be(50m);
    }

    [Fact]
    public void ShippingOrderLine_SetBatch()
    {
        var line = ShippingOrderLine.Create(Guid.NewGuid(), Guid.NewGuid(), 50m);

        line.SetBatch("BATCH-X", "SN-999");

        line.BatchNumber.Should().Be("BATCH-X");
        line.SerialNumber.Should().Be("SN-999");
    }

    [Fact]
    public void ShippingOrderLine_FullFlow()
    {
        var line = ShippingOrderLine.Create(Guid.NewGuid(), Guid.NewGuid(), 100m);

        line.PickedQty.Should().Be(0);

        line.Pick(100m, Guid.NewGuid());
        line.PickedQty.Should().Be(100m);

        line.Pack(100m);
        line.PackedQty.Should().Be(100m);

        line.SetShippedQty(100m);
        line.ShippedQty.Should().Be(100m);
    }
}
