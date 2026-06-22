using System.Globalization;
using GeorgiaERP.Application.Common;
using GeorgiaERP.Infrastructure.Localization;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GeorgiaERP.Tests.Application;

public class LocalizationServiceTests
{
    private readonly ResourceLocalizationService _sut = new(NullLogger<ResourceLocalizationService>.Instance);

    [Theory]
    [InlineData("Label.Product", "Product")]
    [InlineData("Label.Warehouse", "Warehouse")]
    [InlineData("Label.Save", "Save")]
    [InlineData("Label.Export", "Export")]
    [InlineData("Label.Vat", "VAT")]
    public void Get_EnglishCulture_ReturnsEnglishValue(string key, string expected)
    {
        var result = _sut.Get(key, new CultureInfo("en-US"));
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Label.Product", "პროდუქტი")]
    [InlineData("Label.Warehouse", "საწყობი")]
    [InlineData("Label.Save", "შენახვა")]
    [InlineData("Label.Export", "ექსპორტი")]
    [InlineData("Label.Vat", "დღგ")]
    public void Get_GeorgianCulture_ReturnsGeorgianValue(string key, string expected)
    {
        var result = _sut.Get(key, new CultureInfo("ka-GE"));
        result.Should().Be(expected);
    }

    [Fact]
    public void Get_MissingKey_ReturnsKeyItself()
    {
        var result = _sut.Get("NonExistent.Key", new CultureInfo("en-US"));
        result.Should().Be("NonExistent.Key");
    }

    [Fact]
    public void GetFormatted_WithArguments_FormatsCorrectly()
    {
        var result = _sut.GetFormatted("Validation.Required", new CultureInfo("en-US"), "Email");
        result.Should().Be("Email is required.");
    }

    [Fact]
    public void GetFormatted_Georgian_WithArguments_FormatsCorrectly()
    {
        var result = _sut.GetFormatted("Validation.Required", new CultureInfo("ka-GE"), "ელფოსტა");
        result.Should().Be("ელფოსტა სავალდებულოა.");
    }

    [Fact]
    public void GetFormatted_MultipleArguments_FormatsAll()
    {
        var result = _sut.GetFormatted("Validation.MinLength", new CultureInfo("en-US"), "Password", 8);
        result.Should().Be("Password must be at least 8 characters.");
    }

    [Fact]
    public void Get_ValidationMessages_ExistInBothCultures()
    {
        var keys = new[]
        {
            "Validation.Required",
            "Validation.InvalidEmail",
            "Validation.InvalidPhone",
            "Validation.MinLength",
            "Validation.MaxLength",
            "Validation.MustBePositive",
            "Validation.PasswordTooWeak",
        };

        foreach (var key in keys)
        {
            var en = _sut.Get(key, new CultureInfo("en-US"));
            var ka = _sut.Get(key, new CultureInfo("ka-GE"));

            en.Should().NotBe(key, $"English value missing for {key}");
            ka.Should().NotBe(key, $"Georgian value missing for {key}");
            en.Should().NotBe(ka, $"English and Georgian should differ for {key}");
        }
    }

    [Fact]
    public void Get_ErrorMessages_ExistInBothCultures()
    {
        var keys = new[]
        {
            "Error.NotFound",
            "Error.Unauthorized",
            "Error.Forbidden",
            "Error.InvalidCredentials",
            "Error.InsufficientStock",
        };

        foreach (var key in keys)
        {
            var en = _sut.Get(key, new CultureInfo("en-US"));
            var ka = _sut.Get(key, new CultureInfo("ka-GE"));

            en.Should().NotBe(key, $"English value missing for {key}");
            ka.Should().NotBe(key, $"Georgian value missing for {key}");
        }
    }

    [Fact]
    public void Get_Labels_ExistInBothCultures()
    {
        var keys = new[]
        {
            "Label.Product", "Label.Products", "Label.Category",
            "Label.Inventory", "Label.Warehouse", "Label.Customer",
            "Label.Supplier", "Label.Order", "Label.Invoice",
            "Label.Waybill", "Label.Total", "Label.Subtotal",
            "Label.Quantity", "Label.Price", "Label.Status",
        };

        foreach (var key in keys)
        {
            var en = _sut.Get(key, new CultureInfo("en-US"));
            var ka = _sut.Get(key, new CultureInfo("ka-GE"));

            en.Should().NotBe(key, $"English value missing for {key}");
            ka.Should().NotBe(key, $"Georgian value missing for {key}");
        }
    }

    [Fact]
    public void Get_EmailSubjects_ExistInBothCultures()
    {
        var keys = new[]
        {
            "Email.LowStockSubject",
            "Email.WaybillFailedSubject",
            "Email.WelcomeSubject",
            "Email.PasswordResetSubject",
        };

        foreach (var key in keys)
        {
            var en = _sut.Get(key, new CultureInfo("en-US"));
            var ka = _sut.Get(key, new CultureInfo("ka-GE"));

            en.Should().NotBe(key, $"English value missing for {key}");
            ka.Should().NotBe(key, $"Georgian value missing for {key}");
        }
    }

    [Fact]
    public void Get_NotificationStrings_ExistInBothCultures()
    {
        var keys = new[]
        {
            "Notification.LowStockTitle",
            "Notification.LowStockMessage",
            "Notification.OrderPlacedTitle",
            "Notification.WaybillStatusTitle",
        };

        foreach (var key in keys)
        {
            var en = _sut.Get(key, new CultureInfo("en-US"));
            var ka = _sut.Get(key, new CultureInfo("ka-GE"));

            en.Should().NotBe(key, $"English value missing for {key}");
            ka.Should().NotBe(key, $"Georgian value missing for {key}");
        }
    }

    [Fact]
    public void HasKey_ExistingKey_ReturnsTrue()
    {
        _sut.HasKey("Label.Product").Should().BeTrue();
    }

    [Fact]
    public void HasKey_NonExistentKey_ReturnsFalse()
    {
        _sut.HasKey("Nonexistent.Key.12345").Should().BeFalse();
    }
}
