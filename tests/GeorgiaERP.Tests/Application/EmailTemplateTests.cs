using FluentAssertions;
using GeorgiaERP.Application.Common;
using Xunit;

namespace GeorgiaERP.Tests.Application;

public class EmailTemplateTests
{
    [Fact]
    public void WaybillSubmissionFailed_Creates_HighPriority_Email_With_Details()
    {
        var email = EmailTemplates.WaybillSubmissionFailed(
            "admin@test.ge",
            "WB-2024-001",
            "SOAP timeout after 30s",
            3,
            new DateTime(2024, 6, 15, 14, 30, 0));

        email.To.Should().Be("admin@test.ge");
        email.Subject.Should().Contain("WB-2024-001");
        email.Subject.Should().Contain("CRITICAL");
        email.Priority.Should().Be(EmailPriority.High);
        email.Tag.Should().Be("waybill-failure");
        email.HtmlBody.Should().Contain("WB-2024-001");
        email.HtmlBody.Should().Contain("SOAP timeout after 30s");
        email.HtmlBody.Should().Contain("3"); // retry count
        email.PlainTextBody.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void LowStockAlert_OutOfStock_Items_Get_HighPriority()
    {
        var items = new List<LowStockItem>
        {
            new("SKU001", "Test Product 1", "Main Warehouse", 0, 10),
            new("SKU002", "Test Product 2", "Main Warehouse", 5, 20)
        };

        var email = EmailTemplates.LowStockAlert("inventory@test.ge", items);

        email.To.Should().Be("inventory@test.ge");
        email.Subject.Should().Contain("1 out of stock");
        email.Subject.Should().Contain("1 below minimum");
        email.Priority.Should().Be(EmailPriority.High); // has out-of-stock items
        email.Tag.Should().Be("low-stock-alert");
        email.HtmlBody.Should().Contain("SKU001");
        email.HtmlBody.Should().Contain("SKU002");
    }

    [Fact]
    public void LowStockAlert_No_OutOfStock_Gets_NormalPriority()
    {
        var items = new List<LowStockItem>
        {
            new("SKU001", "Test Product", "Warehouse A", 5, 10)
        };

        var email = EmailTemplates.LowStockAlert("inventory@test.ge", items);

        email.Priority.Should().Be(EmailPriority.Normal);
        email.Subject.Should().Contain("0 out of stock");
    }

    [Fact]
    public void UserRegistered_Without_TempPassword_Omits_Password_Section()
    {
        var email = EmailTemplates.UserRegistered(
            "user@test.ge", "testuser", "Test User");

        email.To.Should().Be("user@test.ge");
        email.Subject.Should().Contain("Welcome");
        email.Tag.Should().Be("user-registration");
        email.Priority.Should().Be(EmailPriority.Normal);
        email.HtmlBody.Should().Contain("Test User");
        email.HtmlBody.Should().Contain("testuser");
        email.HtmlBody.Should().NotContain("Temporary Password");
    }

    [Fact]
    public void UserRegistered_With_TempPassword_Includes_Password()
    {
        var email = EmailTemplates.UserRegistered(
            "user@test.ge", "testuser", "Test User", "TempPass123!");

        email.HtmlBody.Should().Contain("TempPass123!");
        email.HtmlBody.Should().Contain("Temporary Password");
    }

    [Fact]
    public void PasswordReset_Includes_ResetToken_And_Link()
    {
        var email = EmailTemplates.PasswordReset(
            "user@test.ge",
            "testuser",
            "ABC123",
            "https://erp.test.ge/reset?token=ABC123");

        email.To.Should().Be("user@test.ge");
        email.Subject.Should().Contain("Password Reset");
        email.Priority.Should().Be(EmailPriority.High);
        email.Tag.Should().Be("password-reset");
        email.HtmlBody.Should().Contain("ABC123");
        email.HtmlBody.Should().Contain("https://erp.test.ge/reset?token=ABC123");
        email.HtmlBody.Should().Contain("testuser");
        email.PlainTextBody.Should().Contain("ABC123");
    }

    [Fact]
    public void All_Templates_Include_Georgian_Text()
    {
        var waybill = EmailTemplates.WaybillSubmissionFailed("a@b.ge", "WB1", "err", 1, DateTime.Now);
        var registration = EmailTemplates.UserRegistered("a@b.ge", "u", "N");
        var reset = EmailTemplates.PasswordReset("a@b.ge", "u", "T", "http://test");

        // All templates should contain Georgian text
        waybill.HtmlBody.Should().Contain("საქართველოს");
        registration.HtmlBody.Should().Contain("საქართველოს");
        reset.HtmlBody.Should().Contain("საქართველოს");
    }

    [Fact]
    public void EmailMessage_Defaults_To_Normal_Priority()
    {
        var msg = new EmailMessage
        {
            To = "test@test.ge",
            Subject = "Test",
            HtmlBody = "<p>Test</p>"
        };

        msg.Priority.Should().Be(EmailPriority.Normal);
        msg.PlainTextBody.Should().BeNull();
        msg.ReplyTo.Should().BeNull();
        msg.Tag.Should().BeNull();
    }
}
