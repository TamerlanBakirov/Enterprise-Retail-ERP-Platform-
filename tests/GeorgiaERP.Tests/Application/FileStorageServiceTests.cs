using System.Text;
using FluentAssertions;
using GeorgiaERP.Infrastructure.FileStorage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace GeorgiaERP.Tests.Application;

public class FileStorageServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly LocalFileStorageService _service;

    public FileStorageServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"erp-file-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        var options = Options.Create(new FileStorageOptions { BasePath = _tempDir });
        _service = new LocalFileStorageService(options, NullLogger<LocalFileStorageService>.Instance);
    }

    [Fact]
    public async Task SaveAsync_CreatesFileAndReturnsPath()
    {
        var content = "Hello, world!";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        var storedName = await _service.SaveAsync(stream, "test.txt", "text/plain");

        storedName.Should().NotBeNullOrEmpty();
        storedName.Should().EndWith(".txt");
        storedName.Should().Contain("/"); // date-based directory
    }

    [Fact]
    public async Task GetAsync_AfterSave_ReturnsFileContent()
    {
        var content = "Test file content";
        using var saveStream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var storedName = await _service.SaveAsync(saveStream, "doc.pdf", "application/pdf");

        var result = await _service.GetAsync(storedName);

        result.Should().NotBeNull();
        result!.ContentType.Should().Be("application/pdf");
        using var reader = new StreamReader(result.Content);
        var readContent = await reader.ReadToEndAsync();
        readContent.Should().Be(content);
    }

    [Fact]
    public async Task GetAsync_WithNonExistentFile_ReturnsNull()
    {
        var result = await _service.GetAsync("non-existent-file.txt");

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ExistingFile_ReturnsTrue()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("delete me"));
        var storedName = await _service.SaveAsync(stream, "delete.txt", "text/plain");

        var deleted = await _service.DeleteAsync(storedName);

        deleted.Should().BeTrue();
        var afterDelete = await _service.GetAsync(storedName);
        afterDelete.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_NonExistentFile_ReturnsFalse()
    {
        var deleted = await _service.DeleteAsync("non-existent.txt");
        deleted.Should().BeFalse();
    }

    [Fact]
    public async Task SaveAsync_PreservesFileExtension()
    {
        using var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF });
        var storedName = await _service.SaveAsync(stream, "photo.jpg", "image/jpeg");

        storedName.Should().EndWith(".jpg");
    }

    [Fact]
    public async Task SaveAsync_MultipleSaves_GenerateUniqueNames()
    {
        using var stream1 = new MemoryStream(Encoding.UTF8.GetBytes("file1"));
        using var stream2 = new MemoryStream(Encoding.UTF8.GetBytes("file2"));

        var name1 = await _service.SaveAsync(stream1, "test.txt", "text/plain");
        var name2 = await _service.SaveAsync(stream2, "test.txt", "text/plain");

        name1.Should().NotBe(name2);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); }
        catch { /* Ignore cleanup failures in tests */ }
    }
}
