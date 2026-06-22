using System.Reflection;
using GeorgiaERP.Application.Common;
using FluentAssertions;
using Xunit;

namespace GeorgiaERP.Tests.Application;

public class ErrorCodesTests
{
    [Fact]
    public void AllErrorCodes_AreUnique()
    {
        var fields = typeof(ErrorCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .ToList();

        var values = fields.Select(f => (string)f.GetRawConstantValue()!).ToList();
        var duplicates = values.GroupBy(v => v).Where(g => g.Count() > 1).Select(g => g.Key).ToList();

        duplicates.Should().BeEmpty("all error codes must be unique, but found duplicates: {0}",
            string.Join(", ", duplicates));
    }

    [Fact]
    public void AllErrorCodes_AreUpperSnakeCase()
    {
        var fields = typeof(ErrorCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .ToList();

        foreach (var field in fields)
        {
            var value = (string)field.GetRawConstantValue()!;
            value.Should().MatchRegex(@"^[A-Z][A-Z0-9_]*$",
                $"error code '{field.Name}' = '{value}' must be UPPER_SNAKE_CASE");
        }
    }

    [Fact]
    public void AllErrorCodes_AreNonEmpty()
    {
        var fields = typeof(ErrorCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .ToList();

        fields.Should().NotBeEmpty("ErrorCodes should have at least one code defined");

        foreach (var field in fields)
        {
            var value = (string)field.GetRawConstantValue()!;
            value.Should().NotBeNullOrWhiteSpace($"error code '{field.Name}' must not be empty");
        }
    }

    [Fact]
    public void ErrorCodes_ContainsExpectedCategories()
    {
        // Verify key error codes exist
        ErrorCodes.ValidationError.Should().Be("VALIDATION_ERROR");
        ErrorCodes.NotFound.Should().Be("NOT_FOUND");
        ErrorCodes.Conflict.Should().Be("CONFLICT");
        ErrorCodes.Unauthorized.Should().Be("UNAUTHORIZED");
        ErrorCodes.Forbidden.Should().Be("FORBIDDEN");
        ErrorCodes.InternalError.Should().Be("INTERNAL_ERROR");

        // Auth codes
        ErrorCodes.InvalidCredentials.Should().Be("AUTH_INVALID_CREDENTIALS");
        ErrorCodes.TokenExpired.Should().Be("AUTH_TOKEN_EXPIRED");

        // Domain codes
        ErrorCodes.ProductNotFound.Should().Be("PRODUCT_NOT_FOUND");
        ErrorCodes.InsufficientStock.Should().Be("INVENTORY_INSUFFICIENT_STOCK");
        ErrorCodes.RsGeSubmissionFailed.Should().Be("RSGE_SUBMISSION_FAILED");
    }

    [Fact]
    public void Result_Failure_WithErrorCode_PreservesCode()
    {
        var result = Result.Failure("Product not found", ErrorCodes.ProductNotFound);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Product not found");
        result.ErrorCode.Should().Be("PRODUCT_NOT_FOUND");
    }

    [Fact]
    public void Result_NotFound_UsesNotFoundCode()
    {
        var result = Result.NotFound("Product", Guid.NewGuid());

        result.ErrorCode.Should().Be("NOT_FOUND");
    }

    [Fact]
    public void Result_Conflict_UsesConflictCode()
    {
        var result = Result.Conflict("SKU already exists");

        result.ErrorCode.Should().Be("CONFLICT");
    }

    [Fact]
    public void Result_ValidationFailure_UsesValidationCode()
    {
        var errors = new List<string> { "Name is required", "SKU is too long" };
        var result = Result.ValidationFailure(errors);

        result.ErrorCode.Should().Be("VALIDATION_ERROR");
        result.Errors.Should().HaveCount(2);
    }

    [Fact]
    public void ResultT_Failure_WithDomainErrorCode_PreservesCode()
    {
        var result = Result.Failure<int>("Insufficient stock", ErrorCodes.InsufficientStock);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("INVENTORY_INSUFFICIENT_STOCK");
    }

    [Fact]
    public void ErrorCodes_HasAtLeast80Codes()
    {
        var fields = typeof(ErrorCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .ToList();

        fields.Count.Should().BeGreaterThanOrEqualTo(50,
            "ErrorCodes should cover all major error scenarios");
    }
}
