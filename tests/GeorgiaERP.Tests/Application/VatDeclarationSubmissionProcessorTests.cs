using FluentAssertions;
using GeorgiaERP.Application.Compliance;
using GeorgiaERP.Domain.Compliance;
using GeorgiaERP.Infrastructure.Persistence;
using GeorgiaERP.Infrastructure.RsGe;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace GeorgiaERP.Tests.Application;

/// <summary>
/// Exercises the RS.GE worker branch that files a VAT declaration: success moves
/// the declaration to Accepted, a business rejection to Rejected, and a network
/// fault is classified transient so the recovery sweep retries.
/// </summary>
public class VatDeclarationSubmissionProcessorTests
{
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"vat-proc-{Guid.NewGuid()}")
            .Options);

    private static async Task<(VatDeclaration Declaration, FiscalDocument Document)> SeedSubmitted(AppDbContext db)
    {
        var declaration = VatDeclaration.Create(
            new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero));
        declaration.SetTotals(170m, 50m);
        declaration.Submit($"VAT-202603-{declaration.Id:N}");

        var document = FiscalDocument.Create(
            FiscalDocumentType.VatDeclaration,
            internalRef: declaration.RsGeReference,
            referenceType: nameof(VatDeclaration),
            referenceId: declaration.Id);
        document.MarkQueued();

        db.VatDeclarations.Add(declaration);
        db.FiscalDocuments.Add(document);
        await db.SaveChangesAsync();
        return (declaration, document);
    }

    private static RsGeSubmissionProcessor NewProcessor(AppDbContext db, IRsGeSoapClient soap) =>
        new(db, soap, Substitute.For<ILogger<RsGeSubmissionProcessor>>());

    [Fact]
    public async Task Process_Success_MarksDeclarationAccepted()
    {
        await using var db = NewContext();
        var (declaration, document) = await SeedSubmitted(db);
        var soap = Substitute.For<IRsGeSoapClient>();
        soap.SubmitVatDeclarationAsync(Arg.Any<RsGeVatDeclarationRequest>())
            .Returns(new RsGeResult(true, "0", null));

        var result = await NewProcessor(db, soap).ProcessAsync(
            new RsGeSubmissionMessage { FiscalDocumentId = document.Id, Operation = RsGeOperation.SubmitVatDeclaration });

        result.Outcome.Should().Be(RsGeSubmissionOutcome.Succeeded);
        (await db.VatDeclarations.FindAsync(declaration.Id))!.Status.Should().Be(VatDeclarationStatus.Accepted);
        (await db.FiscalDocuments.FindAsync(document.Id))!.Status.Should().Be(FiscalDocumentStatus.Confirmed);
    }

    [Fact]
    public async Task Process_BusinessRejection_MarksDeclarationRejected()
    {
        await using var db = NewContext();
        var (declaration, document) = await SeedSubmitted(db);
        var soap = Substitute.For<IRsGeSoapClient>();
        soap.SubmitVatDeclarationAsync(Arg.Any<RsGeVatDeclarationRequest>())
            .Returns(new RsGeResult(false, "E101", "Period already filed"));

        var result = await NewProcessor(db, soap).ProcessAsync(
            new RsGeSubmissionMessage { FiscalDocumentId = document.Id, Operation = RsGeOperation.SubmitVatDeclaration });

        result.Outcome.Should().Be(RsGeSubmissionOutcome.PermanentFailure);
        (await db.VatDeclarations.FindAsync(declaration.Id))!.Status.Should().Be(VatDeclarationStatus.Rejected);
        (await db.FiscalDocuments.FindAsync(document.Id))!.Status.Should().Be(FiscalDocumentStatus.Rejected);
    }

    [Fact]
    public async Task Process_NetworkFault_ClassifiedTransient()
    {
        await using var db = NewContext();
        var (declaration, document) = await SeedSubmitted(db);
        var soap = Substitute.For<IRsGeSoapClient>();
        soap.SubmitVatDeclarationAsync(Arg.Any<RsGeVatDeclarationRequest>())
            .Returns<Task<RsGeResult>>(_ => throw new HttpRequestException("connection refused"));

        var result = await NewProcessor(db, soap).ProcessAsync(
            new RsGeSubmissionMessage { FiscalDocumentId = document.Id, Operation = RsGeOperation.SubmitVatDeclaration });

        result.Outcome.Should().Be(RsGeSubmissionOutcome.TransientFailure);
        // Declaration stays Submitted; the fiscal document is retryable.
        (await db.VatDeclarations.FindAsync(declaration.Id))!.Status.Should().Be(VatDeclarationStatus.Submitted);
        (await db.FiscalDocuments.FindAsync(document.Id))!.Status.Should().Be(FiscalDocumentStatus.Failed);
    }
}
