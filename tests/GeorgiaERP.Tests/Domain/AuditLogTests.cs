using FluentAssertions;
using GeorgiaERP.Domain.Compliance;
using Xunit;

namespace GeorgiaERP.Tests.Domain;

public class AuditLogTests
{
    [Fact]
    public void Create_SetsAllRequiredProperties()
    {
        var userId = Guid.NewGuid();
        var auditLog = AuditLog.Create(
            "Product", "abc-123", AuditAction.Create,
            null, """{"name":"Test"}""", null,
            userId, "192.168.1.1");

        auditLog.EntityType.Should().Be("Product");
        auditLog.EntityId.Should().Be("abc-123");
        auditLog.Action.Should().Be(AuditAction.Create);
        auditLog.OldValues.Should().BeNull();
        auditLog.NewValues.Should().Be("""{"name":"Test"}""");
        auditLog.ChangedProperties.Should().BeNull();
        auditLog.UserId.Should().Be(userId);
        auditLog.IpAddress.Should().Be("192.168.1.1");
    }

    [Fact]
    public void Create_GeneratesUniqueId()
    {
        var a1 = AuditLog.Create("Product", "1", AuditAction.Create, null, null, null, null, null);
        var a2 = AuditLog.Create("Product", "2", AuditAction.Create, null, null, null, null, null);

        a1.Id.Should().NotBe(Guid.Empty);
        a2.Id.Should().NotBe(Guid.Empty);
        a1.Id.Should().NotBe(a2.Id);
    }

    [Fact]
    public void Create_SetsTimestampCloseToUtcNow()
    {
        var before = DateTimeOffset.UtcNow;
        var auditLog = AuditLog.Create("User", "u1", AuditAction.Update, null, null, null, null, null);
        var after = DateTimeOffset.UtcNow;

        auditLog.Timestamp.Should().BeOnOrAfter(before);
        auditLog.Timestamp.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void Create_ForUpdate_CapturesOldAndNewValuesAndChangedProperties()
    {
        var auditLog = AuditLog.Create(
            "Product", "p-1", AuditAction.Update,
            """{"name":"Old"}""",
            """{"name":"New"}""",
            """["name"]""",
            null, null);

        auditLog.Action.Should().Be(AuditAction.Update);
        auditLog.OldValues.Should().Be("""{"name":"Old"}""");
        auditLog.NewValues.Should().Be("""{"name":"New"}""");
        auditLog.ChangedProperties.Should().Be("""["name"]""");
    }

    [Fact]
    public void Create_ForDelete_CapturesOldValues()
    {
        var auditLog = AuditLog.Create(
            "Product", "p-1", AuditAction.Delete,
            """{"name":"Deleted Item"}""",
            null, null, null, null);

        auditLog.Action.Should().Be(AuditAction.Delete);
        auditLog.OldValues.Should().NotBeNull();
        auditLog.NewValues.Should().BeNull();
    }

    [Fact]
    public void Create_WithNullOptionalFields_Succeeds()
    {
        var auditLog = AuditLog.Create(
            "StockLevel", "sl-1", AuditAction.Create,
            null, null, null, null, null);

        auditLog.UserId.Should().BeNull();
        auditLog.IpAddress.Should().BeNull();
        auditLog.OldValues.Should().BeNull();
        auditLog.NewValues.Should().BeNull();
        auditLog.ChangedProperties.Should().BeNull();
    }

    [Theory]
    [InlineData(AuditAction.Create)]
    [InlineData(AuditAction.Update)]
    [InlineData(AuditAction.Delete)]
    public void Create_AllActions_AreSupported(AuditAction action)
    {
        var auditLog = AuditLog.Create("Entity", "1", action, null, null, null, null, null);
        auditLog.Action.Should().Be(action);
    }
}
