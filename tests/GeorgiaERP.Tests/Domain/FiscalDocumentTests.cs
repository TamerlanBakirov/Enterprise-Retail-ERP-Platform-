using FluentAssertions;
using GeorgiaERP.Domain.Compliance;
using Xunit;

namespace GeorgiaERP.Tests.Domain;

public class FiscalDocumentTests
{
    [Fact]
    public void Create_SetsDefaultValues()
    {
        var doc = FiscalDocument.Create(FiscalDocumentType.Waybill, "WB-001", "PurchaseOrder", Guid.NewGuid());

        doc.Id.Should().NotBeEmpty();
        doc.Status.Should().Be(FiscalDocumentStatus.Pending);
        doc.DocumentType.Should().Be(FiscalDocumentType.Waybill);
        doc.RetryCount.Should().Be(0);
        doc.InternalRef.Should().Be("WB-001");
    }

    [Fact]
    public void MarkQueued_FromPending_Succeeds()
    {
        var doc = FiscalDocument.Create(FiscalDocumentType.Invoice);
        doc.MarkQueued();
        doc.Status.Should().Be(FiscalDocumentStatus.Queued);
    }

    [Fact]
    public void MarkSubmitted_FromQueued_Succeeds()
    {
        var doc = FiscalDocument.Create(FiscalDocumentType.Invoice);
        doc.MarkQueued();
        doc.MarkSubmitted("RSGE-123", "INV-001");

        doc.Status.Should().Be(FiscalDocumentStatus.Submitted);
        doc.RsGeId.Should().Be("RSGE-123");
        doc.DocumentNumber.Should().Be("INV-001");
        doc.SubmittedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkConfirmed_FromSubmitted_Succeeds()
    {
        var doc = FiscalDocument.Create(FiscalDocumentType.Waybill);
        doc.MarkQueued();
        doc.MarkSubmitted("RSGE-456");
        doc.MarkConfirmed("ACCEPTED");

        doc.Status.Should().Be(FiscalDocumentStatus.Confirmed);
        doc.RsGeStatus.Should().Be("ACCEPTED");
        doc.ConfirmedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkRejected_FromSubmitted_Succeeds()
    {
        var doc = FiscalDocument.Create(FiscalDocumentType.Invoice);
        doc.MarkQueued();
        doc.MarkSubmitted("RSGE-789");
        doc.MarkRejected("Invalid TIN");

        doc.Status.Should().Be(FiscalDocumentStatus.Rejected);
        doc.LastError.Should().Be("Invalid TIN");
    }

    [Fact]
    public void MarkFailed_FromQueued_AllowsRetry()
    {
        var doc = FiscalDocument.Create(FiscalDocumentType.Waybill);
        doc.MarkQueued();
        doc.MarkFailed("Connection timeout");

        doc.Status.Should().Be(FiscalDocumentStatus.Failed);
        doc.LastError.Should().Be("Connection timeout");

        // Can go back to Queued for retry
        doc.MarkQueued();
        doc.Status.Should().Be(FiscalDocumentStatus.Queued);
    }

    [Fact]
    public void MarkQueued_FromConfirmed_Throws()
    {
        var doc = FiscalDocument.Create(FiscalDocumentType.Invoice);
        doc.MarkQueued();
        doc.MarkSubmitted("RSGE-001");
        doc.MarkConfirmed();

        var act = () => doc.MarkQueued();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Invalid fiscal document state transition*");
    }

    [Fact]
    public void IncrementRetry_IncreasesCount()
    {
        var doc = FiscalDocument.Create(FiscalDocumentType.Waybill);
        doc.IncrementRetry();
        doc.IncrementRetry();
        doc.IncrementRetry();

        doc.RetryCount.Should().Be(3);
    }

    [Fact]
    public void HasExceededRetries_ReturnsTrueWhenOverLimit()
    {
        var doc = FiscalDocument.Create(FiscalDocumentType.Invoice);
        for (var i = 0; i < 10; i++) doc.IncrementRetry();

        doc.HasExceededRetries(10).Should().BeTrue();
        doc.HasExceededRetries(11).Should().BeFalse();
    }

    [Fact]
    public void SetSubmissionDeadline_SetsValue()
    {
        var doc = FiscalDocument.Create(FiscalDocumentType.Waybill);
        var deadline = DateTimeOffset.UtcNow.AddDays(30);
        doc.SetSubmissionDeadline(deadline);

        doc.SubmissionDeadline.Should().Be(deadline);
    }

    [Theory]
    [InlineData(FiscalDocumentType.Waybill)]
    [InlineData(FiscalDocumentType.Invoice)]
    [InlineData(FiscalDocumentType.VatDeclaration)]
    [InlineData(FiscalDocumentType.FiscalReceipt)]
    public void Create_SupportsAllDocumentTypes(FiscalDocumentType type)
    {
        var doc = FiscalDocument.Create(type);
        doc.DocumentType.Should().Be(type);
    }

    [Fact]
    public void FullLifecycle_PendingToConfirmed()
    {
        var doc = FiscalDocument.Create(FiscalDocumentType.Waybill, "WB-TEST");

        doc.SetSubmissionDeadline(DateTimeOffset.UtcNow.AddDays(30));
        doc.SetDocumentData("{\"items\":[]}");
        doc.MarkQueued();
        doc.MarkSubmitted("RSGE-999", "WB-12345");
        doc.MarkConfirmed("CONFIRMED");

        doc.Status.Should().Be(FiscalDocumentStatus.Confirmed);
        doc.RsGeId.Should().Be("RSGE-999");
        doc.DocumentNumber.Should().Be("WB-12345");
        doc.SubmittedAt.Should().NotBeNull();
        doc.ConfirmedAt.Should().NotBeNull();
    }

    [Fact]
    public void FailAndRetryLifecycle()
    {
        var doc = FiscalDocument.Create(FiscalDocumentType.Invoice);

        doc.MarkQueued();
        doc.MarkFailed("Timeout");
        doc.IncrementRetry();

        doc.MarkQueued();
        doc.MarkSubmitted("RSGE-100");
        doc.MarkConfirmed();

        doc.Status.Should().Be(FiscalDocumentStatus.Confirmed);
        doc.RetryCount.Should().Be(1);
    }
}
