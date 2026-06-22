using FluentAssertions;
using GeorgiaERP.Domain.Common;
using Xunit;

namespace GeorgiaERP.Tests.Domain;

public class FileMetadataTests
{
    [Fact]
    public void Create_WithValidData_SetsProperties()
    {
        var uploaderId = Guid.NewGuid();
        var entityId = Guid.NewGuid();

        var file = FileMetadata.Create(
            "document.pdf",
            "2026/06/22/abc123.pdf",
            "application/pdf",
            1024,
            uploaderId,
            "invoice",
            entityId,
            "PurchaseOrder");

        file.FileName.Should().Be("document.pdf");
        file.StoredFileName.Should().Be("2026/06/22/abc123.pdf");
        file.ContentType.Should().Be("application/pdf");
        file.SizeBytes.Should().Be(1024);
        file.UploadedBy.Should().Be(uploaderId);
        file.Category.Should().Be("invoice");
        file.EntityId.Should().Be(entityId);
        file.EntityType.Should().Be("PurchaseOrder");
        file.UploadedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        file.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_WithMinimalData_SetsDefaults()
    {
        var uploaderId = Guid.NewGuid();

        var file = FileMetadata.Create(
            "photo.jpg",
            "stored/photo.jpg",
            "image/jpeg",
            512,
            uploaderId);

        file.Category.Should().BeNull();
        file.EntityId.Should().BeNull();
        file.EntityType.Should().BeNull();
    }

    [Theory]
    [InlineData("", "stored.txt", "text/plain", 100)]
    [InlineData("  ", "stored.txt", "text/plain", 100)]
    public void Create_WithEmptyFileName_Throws(string fileName, string storedName, string contentType, long size)
    {
        var act = () => FileMetadata.Create(fileName, storedName, contentType, size, Guid.NewGuid());
        act.Should().Throw<ArgumentException>().WithMessage("*File name*");
    }

    [Theory]
    [InlineData("file.txt", "", "text/plain", 100)]
    [InlineData("file.txt", "  ", "text/plain", 100)]
    public void Create_WithEmptyStoredFileName_Throws(string fileName, string storedName, string contentType, long size)
    {
        var act = () => FileMetadata.Create(fileName, storedName, contentType, size, Guid.NewGuid());
        act.Should().Throw<ArgumentException>().WithMessage("*Stored file name*");
    }

    [Fact]
    public void Create_WithEmptyContentType_Throws()
    {
        var act = () => FileMetadata.Create("file.txt", "stored.txt", "", 100, Guid.NewGuid());
        act.Should().Throw<ArgumentException>().WithMessage("*Content type*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithInvalidSize_Throws(long size)
    {
        var act = () => FileMetadata.Create("file.txt", "stored.txt", "text/plain", size, Guid.NewGuid());
        act.Should().Throw<ArgumentException>().WithMessage("*size*");
    }

    [Fact]
    public void Create_WithEmptyUploaderId_Throws()
    {
        var act = () => FileMetadata.Create("file.txt", "stored.txt", "text/plain", 100, Guid.Empty);
        act.Should().Throw<ArgumentException>().WithMessage("*Uploader*");
    }

    [Fact]
    public void LinkToEntity_SetsEntityFields()
    {
        var file = FileMetadata.Create("file.txt", "stored.txt", "text/plain", 100, Guid.NewGuid());
        var entityId = Guid.NewGuid();

        file.LinkToEntity(entityId, "Product");

        file.EntityId.Should().Be(entityId);
        file.EntityType.Should().Be("Product");
    }
}
