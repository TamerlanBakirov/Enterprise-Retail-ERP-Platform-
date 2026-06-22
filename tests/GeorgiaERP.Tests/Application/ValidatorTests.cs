using FluentAssertions;
using FluentValidation.TestHelper;
using GeorgiaERP.Application.Compliance;
using GeorgiaERP.Application.Compliance.Commands;
using GeorgiaERP.Application.Compliance.Validators;
using GeorgiaERP.Application.CRM.Commands;
using GeorgiaERP.Application.CRM.Validators;
using GeorgiaERP.Application.Identity.Commands;
using GeorgiaERP.Application.Identity.Validators;
using GeorgiaERP.Application.POS.Commands;
using GeorgiaERP.Application.POS.Validators;
using GeorgiaERP.Application.Products.Commands;
using GeorgiaERP.Application.Products.DTOs;
using GeorgiaERP.Application.Products.Validators;
using GeorgiaERP.Domain.POS;
using Xunit;

namespace GeorgiaERP.Tests.Application;

public class ValidatorTests
{
    // ===== CreateUserCommandValidator =====

    private readonly CreateUserCommandValidator _userValidator = new();

    private static CreateUserCommand ValidUserCommand() => new(
        "testuser", "test@example.com", "StrongPass1!",
        "John", "Doe", "ჯონ", "დოუ", null, null, "ka", []);

    [Fact]
    public void User_Valid_NoErrors()
    {
        var result = _userValidator.TestValidate(ValidUserCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void User_EmptyUsername_Fails(string? username)
    {
        var cmd = ValidUserCommand() with { Username = username! };
        var result = _userValidator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void User_ShortUsername_Fails()
    {
        var cmd = ValidUserCommand() with { Username = "ab" };
        var result = _userValidator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void User_UsernameWithSpaces_Fails()
    {
        var cmd = ValidUserCommand() with { Username = "user name" };
        var result = _userValidator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void User_InvalidEmail_Fails()
    {
        var cmd = ValidUserCommand() with { Email = "not-an-email" };
        var result = _userValidator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("short1!")]
    [InlineData("nouppercase1!")]
    [InlineData("NOLOWERCASE1!")]
    [InlineData("NoDigitsHere!")]
    [InlineData("NoSpecial1char")]
    public void User_WeakPassword_Fails(string password)
    {
        var cmd = ValidUserCommand() with { Password = password };
        var result = _userValidator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void User_InvalidLanguage_Fails()
    {
        var cmd = ValidUserCommand() with { DefaultLanguage = "fr" };
        var result = _userValidator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.DefaultLanguage);
    }

    [Theory]
    [InlineData("ka")]
    [InlineData("en")]
    [InlineData("ru")]
    public void User_ValidLanguages_Pass(string lang)
    {
        var cmd = ValidUserCommand() with { DefaultLanguage = lang };
        var result = _userValidator.TestValidate(cmd);
        result.ShouldNotHaveValidationErrorFor(x => x.DefaultLanguage);
    }

    // ===== LoginCommandValidator =====

    private readonly LoginCommandValidator _loginValidator = new();

    [Fact]
    public void Login_Valid_NoErrors()
    {
        var cmd = new LoginCommand("admin", "password", null, null, null);
        var result = _loginValidator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Login_EmptyUsername_Fails()
    {
        var cmd = new LoginCommand("", "password", null, null, null);
        var result = _loginValidator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void Login_EmptyPassword_Fails()
    {
        var cmd = new LoginCommand("admin", "", null, null, null);
        var result = _loginValidator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    // ===== CreateProductCommandValidator =====

    private readonly CreateProductCommandValidator _productValidator = new();

    private static CreateProductCommand ValidProductCommand() => new(
        "SKU-001", "Product Name", null, null, Guid.NewGuid(), "Piece",
        true, null, null, null, null, null, null, null, null, null,
        false, false, false, null, Guid.NewGuid());

    [Fact]
    public void Product_Valid_NoErrors()
    {
        var result = _productValidator.TestValidate(ValidProductCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Product_EmptySku_Fails(string? sku)
    {
        var cmd = ValidProductCommand() with { Sku = sku! };
        var result = _productValidator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Sku);
    }

    [Fact]
    public void Product_SkuWithSpaces_Fails()
    {
        var cmd = ValidProductCommand() with { Sku = "SKU 001" };
        var result = _productValidator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Sku);
    }

    [Fact]
    public void Product_NegativeWeight_Fails()
    {
        var cmd = ValidProductCommand() with { WeightKg = -1m };
        var result = _productValidator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.WeightKg);
    }

    [Fact]
    public void Product_MaxLessThanMin_Fails()
    {
        var cmd = ValidProductCommand() with { MinStockLevel = 100m, MaxStockLevel = 50m };
        var result = _productValidator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.MaxStockLevel);
    }

    [Fact]
    public void Product_BarcodeWithEmptyValue_Fails()
    {
        var cmd = ValidProductCommand() with
        {
            Barcodes = [new CreateBarcodeRequest("", "EAN13", true)]
        };
        var result = _productValidator.TestValidate(cmd);
        result.ShouldHaveAnyValidationError();
    }

    // ===== CreateCustomerCommandValidator =====

    private readonly CreateCustomerCommandValidator _customerValidator = new();

    [Fact]
    public void Customer_Valid_NoErrors()
    {
        var cmd = new CreateCustomerCommand(
            "John", "Doe", null, null, null, "123456789",
            "+995555123456", "john@test.ge", null, null, false, false);
        var result = _customerValidator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Customer_EmptyFirstName_Fails()
    {
        var cmd = new CreateCustomerCommand(
            "", "Doe", null, null, null, null, null, null, null, null, false, false);
        var result = _customerValidator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void Customer_InvalidTin_Fails()
    {
        var cmd = new CreateCustomerCommand(
            "John", "Doe", null, null, null, "ABC",
            null, null, null, null, false, false);
        var result = _customerValidator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Tin);
    }

    [Fact]
    public void Customer_InvalidEmail_Fails()
    {
        var cmd = new CreateCustomerCommand(
            "John", "Doe", null, null, null, null,
            null, "not-email", null, null, false, false);
        var result = _customerValidator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    // ===== CreatePosTransactionCommandValidator =====

    private readonly CreatePosTransactionCommandValidator _posValidator = new();

    [Fact]
    public void PosTransaction_Valid_NoErrors()
    {
        var cmd = new CreatePosTransactionCommand(
            Guid.NewGuid(), null,
            [new PosLineInput(Guid.NewGuid(), null, 1m, 10m)],
            [new PosPaymentInput(PaymentMethod.Cash, 10m)]);
        var result = _posValidator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void PosTransaction_EmptyLines_Fails()
    {
        var cmd = new CreatePosTransactionCommand(
            Guid.NewGuid(), null, [], [new PosPaymentInput(PaymentMethod.Cash, 10m)]);
        var result = _posValidator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Lines);
    }

    [Fact]
    public void PosTransaction_EmptyPayments_Fails()
    {
        var cmd = new CreatePosTransactionCommand(
            Guid.NewGuid(), null,
            [new PosLineInput(Guid.NewGuid(), null, 1m, 10m)], []);
        var result = _posValidator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Payments);
    }

    [Fact]
    public void PosTransaction_ZeroQuantity_Fails()
    {
        var cmd = new CreatePosTransactionCommand(
            Guid.NewGuid(), null,
            [new PosLineInput(Guid.NewGuid(), null, 0m, 10m)],
            [new PosPaymentInput(PaymentMethod.Cash, 10m)]);
        var result = _posValidator.TestValidate(cmd);
        result.ShouldHaveAnyValidationError();
    }

    [Fact]
    public void PosTransaction_LineWithoutProductOrBarcode_Fails()
    {
        var cmd = new CreatePosTransactionCommand(
            Guid.NewGuid(), null,
            [new PosLineInput(null, null, 1m, 10m)],
            [new PosPaymentInput(PaymentMethod.Cash, 10m)]);
        var result = _posValidator.TestValidate(cmd);
        result.ShouldHaveAnyValidationError();
    }

    [Fact]
    public void PosTransaction_EmptySessionId_Fails()
    {
        var cmd = new CreatePosTransactionCommand(
            Guid.Empty, null,
            [new PosLineInput(Guid.NewGuid(), null, 1m, 10m)],
            [new PosPaymentInput(PaymentMethod.Cash, 10m)]);
        var result = _posValidator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.SessionId);
    }

    // ===== CreateWaybillCommandValidator =====

    private readonly CreateWaybillCommandValidator _waybillValidator = new();

    private static CreateWaybillCommand ValidWaybillCommand() => new(
        1, "123456789", "Buyer", "987654321", "Seller",
        "Tbilisi, Start St", "Batumi, End St",
        "AA-123-BB", "111222333", "Auto", "REF-001", null, null,
        [new WaybillGoodsItem("Product", 1, 10m, 50m, null)]);

    [Fact]
    public void Waybill_Valid_NoErrors()
    {
        var result = _waybillValidator.TestValidate(ValidWaybillCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Waybill_ZeroWaybillType_Fails()
    {
        var cmd = ValidWaybillCommand() with { WaybillType = 0 };
        var result = _waybillValidator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.WaybillType);
    }

    [Fact]
    public void Waybill_EmptyBuyerTin_Fails()
    {
        var cmd = ValidWaybillCommand() with { BuyerTin = "" };
        var result = _waybillValidator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.BuyerTin);
    }

    [Fact]
    public void Waybill_EmptyStartAddress_Fails()
    {
        var cmd = ValidWaybillCommand() with { StartAddress = "" };
        var result = _waybillValidator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.StartAddress);
    }

    [Fact]
    public void Waybill_EmptyGoods_Fails()
    {
        var cmd = ValidWaybillCommand() with { Goods = [] };
        var result = _waybillValidator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Goods);
    }

    [Fact]
    public void Waybill_GoodsWithZeroQuantity_Fails()
    {
        var cmd = ValidWaybillCommand() with
        {
            Goods = [new WaybillGoodsItem("Product", 1, 0m, 50m, null)]
        };
        var result = _waybillValidator.TestValidate(cmd);
        result.ShouldHaveAnyValidationError();
    }

    [Fact]
    public void Waybill_GoodsWithNegativePrice_Fails()
    {
        var cmd = ValidWaybillCommand() with
        {
            Goods = [new WaybillGoodsItem("Product", 1, 5m, -10m, null)]
        };
        var result = _waybillValidator.TestValidate(cmd);
        result.ShouldHaveAnyValidationError();
    }
}
