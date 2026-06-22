using FluentAssertions;
using GeorgiaERP.Domain.Inventory;
using GeorgiaERP.Domain.Inventory.Events;
using Xunit;

namespace GeorgiaERP.Tests.Domain;

public class InventoryTests
{
    private static readonly Guid ProductId = Guid.NewGuid();
    private static readonly Guid WarehouseId = Guid.NewGuid();

    // === StockLevel Tests ===

    [Fact]
    public void StockLevel_Create_InitializesWithZeroQuantities()
    {
        var stock = StockLevel.Create(ProductId, WarehouseId, costPrice: 10m);

        stock.ProductId.Should().Be(ProductId);
        stock.WarehouseId.Should().Be(WarehouseId);
        stock.CostPrice.Should().Be(10m);
        stock.QuantityOnHand.Should().Be(0);
        stock.QuantityReserved.Should().Be(0);
        stock.QuantityInTransit.Should().Be(0);
        stock.AvailableQuantity.Should().Be(0);
    }

    [Fact]
    public void StockLevel_Create_WithVariant()
    {
        var variantId = Guid.NewGuid();
        var stock = StockLevel.Create(ProductId, WarehouseId, variantId: variantId);

        stock.VariantId.Should().Be(variantId);
    }

    [Fact]
    public void AddStock_IncreasesQuantityOnHand()
    {
        var stock = StockLevel.Create(ProductId, WarehouseId);

        stock.AddStock(50m);

        stock.QuantityOnHand.Should().Be(50m);
        stock.AvailableQuantity.Should().Be(50m);
    }

    [Fact]
    public void AddStock_MultipleAdditions_Accumulate()
    {
        var stock = StockLevel.Create(ProductId, WarehouseId);

        stock.AddStock(30m);
        stock.AddStock(20m);
        stock.AddStock(10m);

        stock.QuantityOnHand.Should().Be(60m);
    }

    [Fact]
    public void AddStock_ZeroOrNegative_Throws()
    {
        var stock = StockLevel.Create(ProductId, WarehouseId);

        var actZero = () => stock.AddStock(0m);
        var actNegative = () => stock.AddStock(-5m);

        actZero.Should().Throw<InvalidOperationException>();
        actNegative.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddStock_RaisesDomainEvent()
    {
        var stock = StockLevel.Create(ProductId, WarehouseId);
        stock.ClearDomainEvents();

        stock.AddStock(25m);

        stock.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<StockAdjustedEvent>();

        var evt = (StockAdjustedEvent)stock.DomainEvents[0];
        evt.ProductId.Should().Be(ProductId);
        evt.WarehouseId.Should().Be(WarehouseId);
        evt.QuantityChange.Should().Be(25m);
        evt.NewQuantityOnHand.Should().Be(25m);
        evt.MovementType.Should().Be(MovementType.Receipt);
    }

    [Fact]
    public void Deduct_ReducesQuantityOnHand()
    {
        var stock = StockLevel.Create(ProductId, WarehouseId);
        stock.AddStock(100m);

        stock.Deduct(30m);

        stock.QuantityOnHand.Should().Be(70m);
        stock.AvailableQuantity.Should().Be(70m);
    }

    [Fact]
    public void Deduct_ZeroOrNegative_Throws()
    {
        var stock = StockLevel.Create(ProductId, WarehouseId);
        stock.AddStock(100m);

        var actZero = () => stock.Deduct(0m);
        var actNegative = () => stock.Deduct(-5m);

        actZero.Should().Throw<InvalidOperationException>();
        actNegative.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Deduct_AllowsNegativeStockOnHand()
    {
        // The domain allows deducting more than on-hand (no negative stock prevention at entity level)
        var stock = StockLevel.Create(ProductId, WarehouseId);
        stock.AddStock(10m);

        stock.Deduct(20m);

        stock.QuantityOnHand.Should().Be(-10m);
    }

    [Fact]
    public void Deduct_RaisesDomainEvent_WithNegativeQuantityChange()
    {
        var stock = StockLevel.Create(ProductId, WarehouseId);
        stock.AddStock(100m);
        stock.ClearDomainEvents();

        stock.Deduct(25m);

        var evt = (StockAdjustedEvent)stock.DomainEvents[0];
        evt.QuantityChange.Should().Be(-25m);
        evt.NewQuantityOnHand.Should().Be(75m);
        evt.MovementType.Should().Be(MovementType.Dispatch);
    }

    [Fact]
    public void Deduct_WithCustomMovementType()
    {
        var stock = StockLevel.Create(ProductId, WarehouseId);
        stock.AddStock(100m);
        stock.ClearDomainEvents();

        stock.Deduct(10m, MovementType.Sale);

        var evt = (StockAdjustedEvent)stock.DomainEvents[0];
        evt.MovementType.Should().Be(MovementType.Sale);
    }

    [Fact]
    public void AddStock_BumpsRowVersion()
    {
        var stock = StockLevel.Create(ProductId, WarehouseId);
        var before = stock.RowVersion;

        stock.AddStock(10m);

        stock.RowVersion.Should().NotBeNull();
        stock.RowVersion.Should().NotEqual(before);
    }

    [Fact]
    public void Deduct_BumpsRowVersion()
    {
        var stock = StockLevel.Create(ProductId, WarehouseId);
        stock.AddStock(100m);
        var before = stock.RowVersion;

        stock.Deduct(10m);

        stock.RowVersion.Should().NotEqual(before);
    }

    [Fact]
    public void AvailableQuantity_IsOnHandMinusReserved()
    {
        var stock = StockLevel.Create(ProductId, WarehouseId);
        stock.AddStock(100m);

        // AvailableQuantity = QuantityOnHand - QuantityReserved
        // QuantityReserved is 0 initially
        stock.AvailableQuantity.Should().Be(100m);
    }

    // === StockMovement Tests ===

    [Fact]
    public void StockMovement_Create_SetsAllProperties()
    {
        var createdBy = Guid.NewGuid();
        var movement = StockMovement.Create(
            MovementType.Receipt, ProductId, WarehouseId,
            50m, 10.5m, createdBy);

        movement.MovementType.Should().Be(MovementType.Receipt);
        movement.ProductId.Should().Be(ProductId);
        movement.WarehouseId.Should().Be(WarehouseId);
        movement.Quantity.Should().Be(50m);
        movement.CostPrice.Should().Be(10.5m);
        movement.CreatedBy.Should().Be(createdBy);
        movement.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void StockMovement_Create_WithVariant()
    {
        var variantId = Guid.NewGuid();
        var movement = StockMovement.Create(
            MovementType.Sale, ProductId, WarehouseId,
            -5m, 8m, Guid.NewGuid(), variantId);

        movement.VariantId.Should().Be(variantId);
    }

    [Theory]
    [InlineData(MovementType.Receipt)]
    [InlineData(MovementType.Dispatch)]
    [InlineData(MovementType.TransferIn)]
    [InlineData(MovementType.TransferOut)]
    [InlineData(MovementType.Adjustment)]
    [InlineData(MovementType.Sale)]
    [InlineData(MovementType.Return)]
    public void StockMovement_AllMovementTypes_AreSupported(MovementType type)
    {
        var movement = StockMovement.Create(type, ProductId, WarehouseId, 1m, 1m, Guid.NewGuid());
        movement.MovementType.Should().Be(type);
    }

    // === TransferOrder Tests ===

    [Fact]
    public void TransferOrder_Create_StartsDraft()
    {
        var sourceId = Guid.NewGuid();
        var destId = Guid.NewGuid();
        var requestedBy = Guid.NewGuid();

        var order = TransferOrder.Create("TR-001", sourceId, destId, requestedBy);

        order.TransferNumber.Should().Be("TR-001");
        order.SourceWarehouseId.Should().Be(sourceId);
        order.DestWarehouseId.Should().Be(destId);
        order.RequestedBy.Should().Be(requestedBy);
        order.Status.Should().Be(TransferOrderStatus.Draft);
        order.ApprovedBy.Should().BeNull();
        order.ShippedAt.Should().BeNull();
        order.ReceivedAt.Should().BeNull();
    }

    [Fact]
    public void TransferOrder_Approve_SetsApprovedStatusAndUser()
    {
        var order = TransferOrder.Create("TR-001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var approver = Guid.NewGuid();

        order.Approve(approver);

        order.Status.Should().Be(TransferOrderStatus.Approved);
        order.ApprovedBy.Should().Be(approver);
    }

    [Fact]
    public void TransferOrder_Ship_SetsInTransitStatus()
    {
        var order = TransferOrder.Create("TR-001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        order.Approve(Guid.NewGuid());

        order.Ship();

        order.Status.Should().Be(TransferOrderStatus.InTransit);
        order.ShippedAt.Should().NotBeNull();
        order.ShippedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void TransferOrder_Receive_SetsReceivedStatus()
    {
        var order = TransferOrder.Create("TR-001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        order.Approve(Guid.NewGuid());
        order.Ship();

        order.Receive();

        order.Status.Should().Be(TransferOrderStatus.Received);
        order.ReceivedAt.Should().NotBeNull();
    }

    [Fact]
    public void TransferOrder_Cancel_SetsCancelledStatus()
    {
        var order = TransferOrder.Create("TR-001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        order.Cancel();

        order.Status.Should().Be(TransferOrderStatus.Cancelled);
    }

    [Fact]
    public void TransferOrder_FullLifecycle_DraftToReceived()
    {
        var order = TransferOrder.Create("TR-001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        order.Status.Should().Be(TransferOrderStatus.Draft);

        order.Approve(Guid.NewGuid());
        order.Status.Should().Be(TransferOrderStatus.Approved);

        order.Ship();
        order.Status.Should().Be(TransferOrderStatus.InTransit);

        order.Receive();
        order.Status.Should().Be(TransferOrderStatus.Received);
    }

    [Fact]
    public void TransferOrder_SetNotes_StoresNotes()
    {
        var order = TransferOrder.Create("TR-001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        order.SetNotes("Urgent transfer requested");

        order.Notes.Should().Be("Urgent transfer requested");
    }

    [Fact]
    public void TransferOrder_LinkWaybill_StoresWaybillId()
    {
        var order = TransferOrder.Create("TR-001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var waybillId = Guid.NewGuid();

        order.LinkWaybill(waybillId);

        order.RsGeWaybillId.Should().Be(waybillId);
    }

    // === TransferOrderLine Tests ===

    [Fact]
    public void TransferOrderLine_Create_SetsProperties()
    {
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var line = TransferOrderLine.Create(orderId, productId, 25m);

        line.TransferOrderId.Should().Be(orderId);
        line.ProductId.Should().Be(productId);
        line.RequestedQty.Should().Be(25m);
        line.ShippedQty.Should().BeNull();
        line.ReceivedQty.Should().BeNull();
    }

    [Fact]
    public void TransferOrderLine_SetShippedQty()
    {
        var line = TransferOrderLine.Create(Guid.NewGuid(), Guid.NewGuid(), 25m);

        line.SetShippedQty(25m);

        line.ShippedQty.Should().Be(25m);
    }

    [Fact]
    public void TransferOrderLine_SetReceivedQty()
    {
        var line = TransferOrderLine.Create(Guid.NewGuid(), Guid.NewGuid(), 25m);
        line.SetShippedQty(25m);

        line.SetReceivedQty(24m);

        line.ReceivedQty.Should().Be(24m);
    }

    [Fact]
    public void TransferOrderLine_SetBatch()
    {
        var line = TransferOrderLine.Create(Guid.NewGuid(), Guid.NewGuid(), 10m);

        line.SetBatch("BATCH-001", "SN-12345");

        line.BatchNumber.Should().Be("BATCH-001");
        line.SerialNumber.Should().Be("SN-12345");
    }

    // === StockCount Tests ===

    [Fact]
    public void StockCount_Create_StartsDraft()
    {
        var warehouseId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();

        var count = StockCount.Create(warehouseId, CountType.Full, createdBy);

        count.WarehouseId.Should().Be(warehouseId);
        count.CountType.Should().Be(CountType.Full);
        count.CreatedBy.Should().Be(createdBy);
        count.Status.Should().Be(StockCountStatus.Draft);
        count.StartedAt.Should().BeNull();
        count.CompletedAt.Should().BeNull();
        count.ApprovedBy.Should().BeNull();
    }

    [Theory]
    [InlineData(CountType.Full)]
    [InlineData(CountType.Partial)]
    [InlineData(CountType.Cycle)]
    public void StockCount_AllCountTypes_AreSupported(CountType type)
    {
        var count = StockCount.Create(Guid.NewGuid(), type, Guid.NewGuid());
        count.CountType.Should().Be(type);
    }

    [Fact]
    public void StockCount_Start_TransitionsToInProgress()
    {
        var count = StockCount.Create(Guid.NewGuid(), CountType.Full, Guid.NewGuid());

        count.Start();

        count.Status.Should().Be(StockCountStatus.InProgress);
        count.StartedAt.Should().NotBeNull();
        count.StartedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void StockCount_Complete_TransitionsToCompleted()
    {
        var count = StockCount.Create(Guid.NewGuid(), CountType.Full, Guid.NewGuid());
        count.Start();
        var approver = Guid.NewGuid();

        count.Complete(approver);

        count.Status.Should().Be(StockCountStatus.Completed);
        count.ApprovedBy.Should().Be(approver);
        count.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void StockCount_Cancel_TransitionsToCancelled()
    {
        var count = StockCount.Create(Guid.NewGuid(), CountType.Full, Guid.NewGuid());

        count.Cancel();

        count.Status.Should().Be(StockCountStatus.Cancelled);
    }

    // === StockCountLine Tests ===

    [Fact]
    public void StockCountLine_Create_SetsExpectedQty()
    {
        var countId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var line = StockCountLine.Create(countId, productId, 50m);

        line.StockCountId.Should().Be(countId);
        line.ProductId.Should().Be(productId);
        line.ExpectedQty.Should().Be(50m);
        line.CountedQty.Should().BeNull();
        line.CountedBy.Should().BeNull();
        line.CountedAt.Should().BeNull();
    }

    [Fact]
    public void StockCountLine_RecordCount_SetsCountedValues()
    {
        var line = StockCountLine.Create(Guid.NewGuid(), Guid.NewGuid(), 50m);
        var countedBy = Guid.NewGuid();

        line.RecordCount(48m, countedBy);

        line.CountedQty.Should().Be(48m);
        line.CountedBy.Should().Be(countedBy);
        line.CountedAt.Should().NotBeNull();
    }

    [Fact]
    public void StockCountLine_Variance_PositiveWhenOvercount()
    {
        var line = StockCountLine.Create(Guid.NewGuid(), Guid.NewGuid(), 50m);
        line.RecordCount(55m, Guid.NewGuid());

        line.Variance.Should().Be(5m);
    }

    [Fact]
    public void StockCountLine_Variance_NegativeWhenUndercount()
    {
        var line = StockCountLine.Create(Guid.NewGuid(), Guid.NewGuid(), 50m);
        line.RecordCount(45m, Guid.NewGuid());

        line.Variance.Should().Be(-5m);
    }

    [Fact]
    public void StockCountLine_Variance_ZeroWhenExactMatch()
    {
        var line = StockCountLine.Create(Guid.NewGuid(), Guid.NewGuid(), 50m);
        line.RecordCount(50m, Guid.NewGuid());

        line.Variance.Should().Be(0m);
    }

    [Fact]
    public void StockCountLine_Variance_IsZeroWhenNotCounted()
    {
        var line = StockCountLine.Create(Guid.NewGuid(), Guid.NewGuid(), 50m);

        // When CountedQty is null, variance uses ExpectedQty as fallback
        line.Variance.Should().Be(0m);
    }
}
