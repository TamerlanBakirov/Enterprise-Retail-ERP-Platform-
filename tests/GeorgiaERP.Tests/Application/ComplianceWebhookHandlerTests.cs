using FluentAssertions;
using GeorgiaERP.Application.Compliance;
using GeorgiaERP.Application.Compliance.Commands;
using GeorgiaERP.Application.Webhooks.Commands;
using GeorgiaERP.Domain.Compliance;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace GeorgiaERP.Tests.Application;

public class ComplianceWebhookHandlerTests
{
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"compliance-wh-{Guid.NewGuid()}")
            .Options);

    // === CreateWaybill ===

    [Fact]
    public async Task CreateWaybill_Valid_PersistsAndQueues()
    {
        await using var db = NewContext();
        var publisher = Substitute.For<IRsGeQueuePublisher>();
        var logger = Substitute.For<ILogger<CreateWaybillCommandHandler>>();
        var handler = new CreateWaybillCommandHandler(db, publisher, logger);

        var result = await handler.Handle(new CreateWaybillCommand(
            WaybillType: 1,
            BuyerTin: "123456789",
            BuyerName: "Buyer LLC",
            SellerTin: "987654321",
            SellerName: "Seller LLC",
            StartAddress: "Tbilisi",
            EndAddress: "Batumi",
            VehicleNumber: "AA-001-BB",
            DriverTin: "111222333",
            TransportType: "Auto",
            InternalRef: "WB-001",
            ReferenceId: null,
            ReferenceType: null,
            Goods: [new WaybillGoodsItem("Product A", 1, 10m, 25m, "1234567890")]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("Queued");
        (await db.FiscalDocuments.CountAsync()).Should().Be(1);
        (await db.RsGeWaybills.CountAsync()).Should().Be(1);

        await publisher.Received(1).PublishAsync(
            Arg.Is<RsGeSubmissionMessage>(m => m.Operation == RsGeOperation.SubmitWaybill),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateWaybill_EmptyGoods_ReturnsFailure()
    {
        await using var db = NewContext();
        var publisher = Substitute.For<IRsGeQueuePublisher>();
        var logger = Substitute.For<ILogger<CreateWaybillCommandHandler>>();
        var handler = new CreateWaybillCommandHandler(db, publisher, logger);

        var result = await handler.Handle(new CreateWaybillCommand(
            WaybillType: 1,
            BuyerTin: "123456789",
            BuyerName: null,
            SellerTin: null,
            SellerName: null,
            StartAddress: "A",
            EndAddress: "B",
            VehicleNumber: null,
            DriverTin: null,
            TransportType: null,
            InternalRef: null,
            ReferenceId: null,
            ReferenceType: null,
            Goods: []),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("at least one");
    }

    [Fact]
    public async Task CreateWaybill_PublisherFails_StillPersists()
    {
        await using var db = NewContext();
        var publisher = Substitute.For<IRsGeQueuePublisher>();
        publisher.PublishAsync(Arg.Any<RsGeSubmissionMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new Exception("Broker down")));

        var logger = Substitute.For<ILogger<CreateWaybillCommandHandler>>();
        var handler = new CreateWaybillCommandHandler(db, publisher, logger);

        var result = await handler.Handle(new CreateWaybillCommand(
            WaybillType: 1,
            BuyerTin: "123456789",
            BuyerName: "Buyer",
            SellerTin: null,
            SellerName: null,
            StartAddress: "A",
            EndAddress: "B",
            VehicleNumber: null,
            DriverTin: null,
            TransportType: null,
            InternalRef: "WB-FAIL",
            ReferenceId: null,
            ReferenceType: null,
            Goods: [new WaybillGoodsItem("Item", 1, 5m, 10m, null)]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await db.FiscalDocuments.CountAsync()).Should().Be(1);
    }

    // === EnqueueWaybillOperation ===

    [Fact]
    public async Task EnqueueOperation_SubmitWaybill_Rejected()
    {
        await using var db = NewContext();
        var publisher = Substitute.For<IRsGeQueuePublisher>();
        var handler = new EnqueueWaybillOperationCommandHandler(db, publisher);

        var result = await handler.Handle(
            new EnqueueWaybillOperationCommand(Guid.NewGuid(), RsGeOperation.SubmitWaybill),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("create endpoints");
    }

    [Fact]
    public async Task EnqueueOperation_DocumentNotFound_ReturnsFailure()
    {
        await using var db = NewContext();
        var publisher = Substitute.For<IRsGeQueuePublisher>();
        var handler = new EnqueueWaybillOperationCommandHandler(db, publisher);

        var result = await handler.Handle(
            new EnqueueWaybillOperationCommand(Guid.NewGuid(), RsGeOperation.ConfirmWaybill),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task EnqueueOperation_NotSubmitted_ReturnsFailure()
    {
        await using var db = NewContext();
        var doc = FiscalDocument.Create(FiscalDocumentType.Waybill, "WB-TEST");
        db.FiscalDocuments.Add(doc);
        await db.SaveChangesAsync();

        var publisher = Substitute.For<IRsGeQueuePublisher>();
        var handler = new EnqueueWaybillOperationCommandHandler(db, publisher);

        var result = await handler.Handle(
            new EnqueueWaybillOperationCommand(doc.Id, RsGeOperation.ConfirmWaybill),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not been submitted");
    }

    [Fact]
    public async Task EnqueueOperation_Valid_PublishesMessage()
    {
        await using var db = NewContext();
        var doc = FiscalDocument.Create(FiscalDocumentType.Waybill, "WB-VALID");
        doc.MarkQueued();
        doc.MarkSubmitted("RS-GE-12345");
        db.FiscalDocuments.Add(doc);
        await db.SaveChangesAsync();

        var publisher = Substitute.For<IRsGeQueuePublisher>();
        var handler = new EnqueueWaybillOperationCommandHandler(db, publisher);

        var result = await handler.Handle(
            new EnqueueWaybillOperationCommand(doc.Id, RsGeOperation.ConfirmWaybill),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await publisher.Received(1).PublishAsync(
            Arg.Is<RsGeSubmissionMessage>(m =>
                m.FiscalDocumentId == doc.Id && m.Operation == RsGeOperation.ConfirmWaybill),
            Arg.Any<CancellationToken>());
    }

    // === Webhook CRUD ===

    [Fact]
    public async Task CreateWebhook_Valid_ReturnsDto()
    {
        await using var db = NewContext();
        var handler = new CreateWebhookCommandHandler(db);

        var result = await handler.Handle(new CreateWebhookCommand(
            "Order Webhook", "https://example.com/hook", "secret123",
            ["order.created", "order.updated"], 5),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Order Webhook");
        result.Value.IsActive.Should().BeTrue();
        result.Value.EventTypes.Should().Contain("order.created");
        (await db.WebhookSubscriptions.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task UpdateWebhook_NotFound_ReturnsFailure()
    {
        await using var db = NewContext();
        var handler = new UpdateWebhookCommandHandler(db);

        var result = await handler.Handle(new UpdateWebhookCommand(
            Guid.NewGuid(), "New Name", null, null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteWebhook_Existing_Removes()
    {
        await using var db = NewContext();
        var createHandler = new CreateWebhookCommandHandler(db);
        var createResult = await createHandler.Handle(new CreateWebhookCommand(
            "ToDelete", "https://example.com", "s", ["test.event"]),
            CancellationToken.None);

        var deleteHandler = new DeleteWebhookCommandHandler(db);
        var result = await deleteHandler.Handle(
            new DeleteWebhookCommand(createResult.Value!.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await db.WebhookSubscriptions.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ActivateDeactivate_Webhook_TogglesState()
    {
        await using var db = NewContext();
        var createHandler = new CreateWebhookCommandHandler(db);
        var createResult = await createHandler.Handle(new CreateWebhookCommand(
            "Toggle", "https://example.com", "s", ["test.event"]),
            CancellationToken.None);

        var id = createResult.Value!.Id;

        var deactivateHandler = new DeactivateWebhookCommandHandler(db);
        var deactivateResult = await deactivateHandler.Handle(new DeactivateWebhookCommand(id), CancellationToken.None);
        deactivateResult.IsSuccess.Should().BeTrue();

        var webhook = await db.WebhookSubscriptions.FindAsync(id);
        webhook!.IsActive.Should().BeFalse();

        var activateHandler = new ActivateWebhookCommandHandler(db);
        var activateResult = await activateHandler.Handle(new ActivateWebhookCommand(id), CancellationToken.None);
        activateResult.IsSuccess.Should().BeTrue();

        await db.Entry(webhook).ReloadAsync();
        webhook.IsActive.Should().BeTrue();
    }
}
