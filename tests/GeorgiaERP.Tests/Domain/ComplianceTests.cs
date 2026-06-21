using FluentAssertions;
using GeorgiaERP.Domain.Compliance;
using GeorgiaERP.Domain.Compliance.Events;
using Xunit;

namespace GeorgiaERP.Tests.Domain;

public class ComplianceTests
{
    // === FiscalDocument ===

    [Fact]
    public void CreateFiscalDocument_SetsDefaultValues()
    {
        var doc = FiscalDocument.Create(FiscalDocumentType.Waybill, "INV-001", "PurchaseOrder", Guid.NewGuid());

        doc.DocumentType.Should().Be(FiscalDocumentType.Waybill);
        doc.InternalRef.Should().Be("INV-001");
        doc.Status.Should().Be(FiscalDocumentStatus.Pending);
        doc.RetryCount.Should().Be(0);
        doc.RsGeId.Should().BeNull();
        doc.LastError.Should().BeNull();
        doc.SubmittedAt.Should().BeNull();
        doc.ConfirmedAt.Should().BeNull();
    }

    [Fact]
    public void FiscalDocument_SetDocumentData_UpdatesData()
    {
        var doc = FiscalDocument.Create(FiscalDocumentType.Invoice);
        var json = "{\"items\": []}";

        doc.SetDocumentData(json);

        doc.DocumentData.Should().Be(json);
    }

    [Fact]
    public void FiscalDocument_SetSubmissionDeadline_UpdatesDeadline()
    {
        var doc = FiscalDocument.Create(FiscalDocumentType.Waybill);
        var deadline = DateTimeOffset.UtcNow.AddDays(5);

        doc.SetSubmissionDeadline(deadline);

        doc.SubmissionDeadline.Should().Be(deadline);
    }

    [Fact]
    public void FiscalDocument_ValidLifecycle_PendingToConfirmed()
    {
        var doc = FiscalDocument.Create(FiscalDocumentType.Waybill);

        doc.MarkQueued();
        doc.Status.Should().Be(FiscalDocumentStatus.Queued);

        doc.MarkSubmitted("RSGE-12345", "WB-001");
        doc.Status.Should().Be(FiscalDocumentStatus.Submitted);
        doc.RsGeId.Should().Be("RSGE-12345");
        doc.DocumentNumber.Should().Be("WB-001");
        doc.SubmittedAt.Should().NotBeNull();

        doc.MarkConfirmed("accepted");
        doc.Status.Should().Be(FiscalDocumentStatus.Confirmed);
        doc.RsGeStatus.Should().Be("accepted");
        doc.ConfirmedAt.Should().NotBeNull();
        doc.LastError.Should().BeNull();
    }

    [Fact]
    public void FiscalDocument_FailedAndRetry_TransitionsCorrectly()
    {
        var doc = FiscalDocument.Create(FiscalDocumentType.Invoice);

        doc.MarkQueued();
        doc.MarkFailed("Connection timeout");
        doc.Status.Should().Be(FiscalDocumentStatus.Failed);
        doc.LastError.Should().Be("Connection timeout");

        // Can retry: Failed -> Queued
        doc.MarkQueued();
        doc.Status.Should().Be(FiscalDocumentStatus.Queued);
    }

    [Fact]
    public void FiscalDocument_Rejected_IsTerminal()
    {
        var doc = FiscalDocument.Create(FiscalDocumentType.Waybill);
        doc.MarkQueued();

        doc.MarkRejected("Invalid TIN");

        doc.Status.Should().Be(FiscalDocumentStatus.Rejected);
        doc.LastError.Should().Be("Invalid TIN");
    }

    [Fact]
    public void FiscalDocument_InvalidTransition_ThrowsException()
    {
        var doc = FiscalDocument.Create(FiscalDocumentType.Waybill);

        // Cannot go directly from Pending to Submitted
        var act = () => doc.MarkSubmitted("RSGE-001");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Invalid fiscal document state transition*");
    }

    [Fact]
    public void FiscalDocument_ConfirmedIsTerminal_CannotTransition()
    {
        var doc = FiscalDocument.Create(FiscalDocumentType.Waybill);
        doc.MarkQueued();
        doc.MarkSubmitted("RSGE-001");
        doc.MarkConfirmed();

        var act = () => doc.MarkFailed("error");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void FiscalDocument_IncrementRetry_IncreasesCount()
    {
        var doc = FiscalDocument.Create(FiscalDocumentType.Waybill);

        doc.IncrementRetry();
        doc.IncrementRetry();
        doc.IncrementRetry();

        doc.RetryCount.Should().Be(3);
    }

    [Fact]
    public void FiscalDocument_HasExceededRetries_ReturnsCorrectly()
    {
        var doc = FiscalDocument.Create(FiscalDocumentType.Waybill);

        doc.HasExceededRetries(3).Should().BeFalse();

        doc.IncrementRetry();
        doc.IncrementRetry();
        doc.IncrementRetry();

        doc.HasExceededRetries(3).Should().BeTrue();
    }

    [Theory]
    [InlineData(FiscalDocumentType.Waybill)]
    [InlineData(FiscalDocumentType.Invoice)]
    [InlineData(FiscalDocumentType.VatDeclaration)]
    [InlineData(FiscalDocumentType.FiscalReceipt)]
    public void FiscalDocument_AllTypes_CanBeCreated(FiscalDocumentType type)
    {
        var doc = FiscalDocument.Create(type);

        doc.DocumentType.Should().Be(type);
    }

    // === RsGeWaybill ===

    [Fact]
    public void CreateWaybill_SetsDefaultValues()
    {
        var fiscalDocId = Guid.NewGuid();
        var waybill = RsGeWaybill.Create(fiscalDocId, "inner");

        waybill.FiscalDocumentId.Should().Be(fiscalDocId);
        waybill.WaybillType.Should().Be("inner");
        waybill.Status.Should().Be(WaybillStatus.Draft);
        waybill.WaybillNumber.Should().BeNull();
        waybill.TotalAmount.Should().BeNull();
    }

    [Fact]
    public void Waybill_SetParties_UpdatesPartyInfo()
    {
        var waybill = RsGeWaybill.Create(Guid.NewGuid());

        waybill.SetParties("111111111", "Seller LLC", "222222222", "Buyer LLC");

        waybill.SellerTin.Should().Be("111111111");
        waybill.SellerName.Should().Be("Seller LLC");
        waybill.BuyerTin.Should().Be("222222222");
        waybill.BuyerName.Should().Be("Buyer LLC");
    }

    [Fact]
    public void Waybill_SetTransport_UpdatesTransportInfo()
    {
        var waybill = RsGeWaybill.Create(Guid.NewGuid());

        waybill.SetTransport("333333333", "auto", "AA-123-BB", "444444444",
            "Tbilisi, Start St", "Kutaisi, End St");

        waybill.TransporterTin.Should().Be("333333333");
        waybill.TransportType.Should().Be("auto");
        waybill.VehicleNumber.Should().Be("AA-123-BB");
        waybill.DriverTin.Should().Be("444444444");
        waybill.StartAddress.Should().Be("Tbilisi, Start St");
        waybill.EndAddress.Should().Be("Kutaisi, End St");
    }

    [Fact]
    public void Waybill_SetGoods_RaisesSubmittedEvent()
    {
        var waybill = RsGeWaybill.Create(Guid.NewGuid());
        waybill.SetParties("111", "Seller", "222", "Buyer");

        waybill.SetGoods("[{\"name\":\"Apples\"}]", 1500m);

        waybill.GoodsData.Should().Contain("Apples");
        waybill.TotalAmount.Should().Be(1500m);
        waybill.DomainEvents.Should().HaveCount(1);
        waybill.DomainEvents[0].Should().BeOfType<WaybillSubmittedEvent>();
    }

    [Fact]
    public void Waybill_ValidLifecycle_DraftToClosed()
    {
        var waybill = RsGeWaybill.Create(Guid.NewGuid());

        waybill.MarkSaved("WB-001");
        waybill.Status.Should().Be(WaybillStatus.Saved);
        waybill.WaybillNumber.Should().Be("WB-001");

        waybill.MarkActive(DateTimeOffset.UtcNow);
        waybill.Status.Should().Be(WaybillStatus.Active);
        waybill.ActivateDate.Should().NotBeNull();

        waybill.MarkConfirmed();
        waybill.Status.Should().Be(WaybillStatus.Confirmed);
        waybill.DomainEvents.Should().Contain(e => e is WaybillConfirmedEvent);

        waybill.MarkClosed(DateTimeOffset.UtcNow);
        waybill.Status.Should().Be(WaybillStatus.Closed);
        waybill.DeliveryDate.Should().NotBeNull();
    }

    [Fact]
    public void Waybill_ActiveToClosed_DirectlyValid()
    {
        var waybill = RsGeWaybill.Create(Guid.NewGuid());
        waybill.MarkSaved("WB-001");
        waybill.MarkActive(DateTimeOffset.UtcNow);

        waybill.MarkClosed(DateTimeOffset.UtcNow);

        waybill.Status.Should().Be(WaybillStatus.Closed);
    }

    [Fact]
    public void Waybill_CanBeRejected_FromDraft()
    {
        var waybill = RsGeWaybill.Create(Guid.NewGuid());

        waybill.MarkRejected();

        waybill.Status.Should().Be(WaybillStatus.Rejected);
    }

    [Fact]
    public void Waybill_CanBeRejected_FromSaved()
    {
        var waybill = RsGeWaybill.Create(Guid.NewGuid());
        waybill.MarkSaved("WB-001");

        waybill.MarkRejected();

        waybill.Status.Should().Be(WaybillStatus.Rejected);
    }

    [Fact]
    public void Waybill_CanBeRejected_FromActive()
    {
        var waybill = RsGeWaybill.Create(Guid.NewGuid());
        waybill.MarkSaved("WB-001");
        waybill.MarkActive(DateTimeOffset.UtcNow);

        waybill.MarkRejected();

        waybill.Status.Should().Be(WaybillStatus.Rejected);
    }

    [Fact]
    public void Waybill_InvalidTransition_FromDraftToActive_Throws()
    {
        var waybill = RsGeWaybill.Create(Guid.NewGuid());

        var act = () => waybill.MarkActive(DateTimeOffset.UtcNow);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Invalid waybill state transition*");
    }

    [Fact]
    public void Waybill_InvalidTransition_FromClosedToConfirmed_Throws()
    {
        var waybill = RsGeWaybill.Create(Guid.NewGuid());
        waybill.MarkSaved("WB-001");
        waybill.MarkActive(DateTimeOffset.UtcNow);
        waybill.MarkClosed(DateTimeOffset.UtcNow);

        var act = () => waybill.MarkConfirmed();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Waybill_InvalidTransition_FromRejected_Throws()
    {
        var waybill = RsGeWaybill.Create(Guid.NewGuid());
        waybill.MarkRejected();

        var act = () => waybill.MarkSaved("WB-002");

        act.Should().Throw<InvalidOperationException>();
    }
}
