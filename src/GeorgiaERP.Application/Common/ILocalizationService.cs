using System.Globalization;

namespace GeorgiaERP.Application.Common;

/// <summary>
/// Provides localized strings for the ERP platform.
/// Supports en-US and ka-GE cultures.
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Gets a localized string by key using the current culture.
    /// </summary>
    string Get(string key);

    /// <summary>
    /// Gets a localized string by key for a specific culture.
    /// </summary>
    string Get(string key, CultureInfo culture);

    /// <summary>
    /// Gets a localized string by key with format arguments using the current culture.
    /// </summary>
    string GetFormatted(string key, params object[] args);

    /// <summary>
    /// Gets a localized string by key with format arguments for a specific culture.
    /// </summary>
    string GetFormatted(string key, CultureInfo culture, params object[] args);

    /// <summary>
    /// Checks if a localization key exists.
    /// </summary>
    bool HasKey(string key);
}

/// <summary>
/// Well-known localization key groups for the ERP platform.
/// </summary>
public static class LocalizationKeys
{
    // Validation messages
    public static class Validation
    {
        public const string Required = "Validation.Required";
        public const string InvalidEmail = "Validation.InvalidEmail";
        public const string InvalidPhone = "Validation.InvalidPhone";
        public const string MinLength = "Validation.MinLength";
        public const string MaxLength = "Validation.MaxLength";
        public const string MustBePositive = "Validation.MustBePositive";
        public const string InvalidRange = "Validation.InvalidRange";
        public const string DuplicateValue = "Validation.DuplicateValue";
        public const string InvalidSku = "Validation.InvalidSku";
        public const string InvalidBarcode = "Validation.InvalidBarcode";
        public const string InvalidTin = "Validation.InvalidTin";
        public const string PasswordTooWeak = "Validation.PasswordTooWeak";
    }

    // API error messages
    public static class Errors
    {
        public const string NotFound = "Error.NotFound";
        public const string Unauthorized = "Error.Unauthorized";
        public const string Forbidden = "Error.Forbidden";
        public const string Conflict = "Error.Conflict";
        public const string InternalError = "Error.InternalError";
        public const string RateLimitExceeded = "Error.RateLimitExceeded";
        public const string InvalidCredentials = "Error.InvalidCredentials";
        public const string AccountLocked = "Error.AccountLocked";
        public const string TokenExpired = "Error.TokenExpired";
        public const string InsufficientStock = "Error.InsufficientStock";
        public const string InvalidTransferState = "Error.InvalidTransferState";
        public const string JournalNotBalanced = "Error.JournalNotBalanced";
        public const string WaybillSubmissionFailed = "Error.WaybillSubmissionFailed";
    }

    // Common labels
    public static class Labels
    {
        public const string Product = "Label.Product";
        public const string Products = "Label.Products";
        public const string Category = "Label.Category";
        public const string Inventory = "Label.Inventory";
        public const string Warehouse = "Label.Warehouse";
        public const string Customer = "Label.Customer";
        public const string Customers = "Label.Customers";
        public const string Supplier = "Label.Supplier";
        public const string Order = "Label.Order";
        public const string Invoice = "Label.Invoice";
        public const string Waybill = "Label.Waybill";
        public const string Total = "Label.Total";
        public const string Subtotal = "Label.Subtotal";
        public const string Vat = "Label.Vat";
        public const string Quantity = "Label.Quantity";
        public const string Price = "Label.Price";
        public const string Status = "Label.Status";
        public const string Date = "Label.Date";
        public const string Actions = "Label.Actions";
        public const string Save = "Label.Save";
        public const string Cancel = "Label.Cancel";
        public const string Delete = "Label.Delete";
        public const string Edit = "Label.Edit";
        public const string Search = "Label.Search";
        public const string Export = "Label.Export";
        public const string Import = "Label.Import";
    }

    // Email template strings
    public static class Email
    {
        public const string LowStockSubject = "Email.LowStockSubject";
        public const string WaybillFailedSubject = "Email.WaybillFailedSubject";
        public const string WelcomeSubject = "Email.WelcomeSubject";
        public const string PasswordResetSubject = "Email.PasswordResetSubject";
    }

    // Notification strings
    public static class Notifications
    {
        public const string LowStockTitle = "Notification.LowStockTitle";
        public const string LowStockMessage = "Notification.LowStockMessage";
        public const string OrderPlacedTitle = "Notification.OrderPlacedTitle";
        public const string WaybillStatusTitle = "Notification.WaybillStatusTitle";
    }
}
