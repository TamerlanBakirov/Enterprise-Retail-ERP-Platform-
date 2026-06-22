namespace GeorgiaERP.Application.Common;

/// <summary>
/// Application-wide error codes for consistent client-side error handling.
/// Each code maps to a specific validation error, business rule violation,
/// or system error. Returned in ProblemDetails responses.
/// </summary>
public static class ErrorCodes
{
    // ── General ──────────────────────────────────────────────────
    public const string ValidationError = "VALIDATION_ERROR";
    public const string NotFound = "NOT_FOUND";
    public const string Conflict = "CONFLICT";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string InternalError = "INTERNAL_ERROR";
    public const string RateLimitExceeded = "RATE_LIMIT_EXCEEDED";

    // ── Authentication ───────────────────────────────────────────
    public const string InvalidCredentials = "AUTH_INVALID_CREDENTIALS";
    public const string AccountLocked = "AUTH_ACCOUNT_LOCKED";
    public const string AccountDisabled = "AUTH_ACCOUNT_DISABLED";
    public const string TokenExpired = "AUTH_TOKEN_EXPIRED";
    public const string TokenInvalid = "AUTH_TOKEN_INVALID";
    public const string RefreshTokenExpired = "AUTH_REFRESH_TOKEN_EXPIRED";
    public const string RefreshTokenRevoked = "AUTH_REFRESH_TOKEN_REVOKED";
    public const string TwoFactorRequired = "AUTH_2FA_REQUIRED";
    public const string TwoFactorInvalid = "AUTH_2FA_INVALID_CODE";
    public const string TwoFactorAlreadyEnabled = "AUTH_2FA_ALREADY_ENABLED";
    public const string TwoFactorNotEnabled = "AUTH_2FA_NOT_ENABLED";

    // ── Users ────────────────────────────────────────────────────
    public const string UserNotFound = "USER_NOT_FOUND";
    public const string UsernameTaken = "USER_USERNAME_TAKEN";
    public const string EmailTaken = "USER_EMAIL_TAKEN";
    public const string PasswordTooWeak = "USER_PASSWORD_TOO_WEAK";
    public const string CannotDeleteSelf = "USER_CANNOT_DELETE_SELF";

    // ── Products ─────────────────────────────────────────────────
    public const string ProductNotFound = "PRODUCT_NOT_FOUND";
    public const string ProductSkuExists = "PRODUCT_SKU_EXISTS";
    public const string ProductInactive = "PRODUCT_INACTIVE";
    public const string CategoryNotFound = "CATEGORY_NOT_FOUND";
    public const string CategoryHasProducts = "CATEGORY_HAS_PRODUCTS";

    // ── Inventory ────────────────────────────────────────────────
    public const string InsufficientStock = "INVENTORY_INSUFFICIENT_STOCK";
    public const string StockLevelNotFound = "INVENTORY_STOCK_LEVEL_NOT_FOUND";
    public const string NegativeQuantity = "INVENTORY_NEGATIVE_QUANTITY";
    public const string TransferOrderNotFound = "INVENTORY_TRANSFER_NOT_FOUND";
    public const string TransferOrderInvalidState = "INVENTORY_TRANSFER_INVALID_STATE";
    public const string StockCountNotFound = "INVENTORY_STOCK_COUNT_NOT_FOUND";
    public const string StockCountInvalidState = "INVENTORY_STOCK_COUNT_INVALID_STATE";

    // ── Warehouse ────────────────────────────────────────────────
    public const string WarehouseNotFound = "WAREHOUSE_NOT_FOUND";
    public const string WarehouseInactive = "WAREHOUSE_INACTIVE";
    public const string WarehouseLocationNotFound = "WAREHOUSE_LOCATION_NOT_FOUND";
    public const string ReceivingOrderNotFound = "WAREHOUSE_RECEIVING_NOT_FOUND";
    public const string ShippingOrderNotFound = "WAREHOUSE_SHIPPING_NOT_FOUND";

    // ── Procurement ──────────────────────────────────────────────
    public const string SupplierNotFound = "PROCUREMENT_SUPPLIER_NOT_FOUND";
    public const string PurchaseOrderNotFound = "PROCUREMENT_PO_NOT_FOUND";
    public const string PurchaseOrderInvalidState = "PROCUREMENT_PO_INVALID_STATE";
    public const string GoodsReceiptNotFound = "PROCUREMENT_GRN_NOT_FOUND";

    // ── POS ──────────────────────────────────────────────────────
    public const string PosSessionNotFound = "POS_SESSION_NOT_FOUND";
    public const string PosSessionClosed = "POS_SESSION_CLOSED";
    public const string PosTransactionNotFound = "POS_TRANSACTION_NOT_FOUND";
    public const string PosTransactionAlreadyVoided = "POS_TRANSACTION_ALREADY_VOIDED";
    public const string PosPaymentInsufficient = "POS_PAYMENT_INSUFFICIENT";

    // ── Finance ──────────────────────────────────────────────────
    public const string AccountNotFound = "FINANCE_ACCOUNT_NOT_FOUND";
    public const string AccountCodeExists = "FINANCE_ACCOUNT_CODE_EXISTS";
    public const string JournalEntryNotFound = "FINANCE_JOURNAL_NOT_FOUND";
    public const string JournalEntryUnbalanced = "FINANCE_JOURNAL_UNBALANCED";
    public const string JournalEntryAlreadyPosted = "FINANCE_JOURNAL_ALREADY_POSTED";
    public const string BankAccountNotFound = "FINANCE_BANK_ACCOUNT_NOT_FOUND";

    // ── Pricing ──────────────────────────────────────────────────
    public const string PriceListNotFound = "PRICING_LIST_NOT_FOUND";
    public const string PromotionNotFound = "PRICING_PROMOTION_NOT_FOUND";
    public const string PromotionOverlap = "PRICING_PROMOTION_OVERLAP";

    // ── Customers ────────────────────────────────────────────────
    public const string CustomerNotFound = "CUSTOMER_NOT_FOUND";
    public const string CustomerNumberExists = "CUSTOMER_NUMBER_EXISTS";

    // ── RS.GE Compliance ─────────────────────────────────────────
    public const string RsGeSubmissionFailed = "RSGE_SUBMISSION_FAILED";
    public const string RsGeConnectionError = "RSGE_CONNECTION_ERROR";
    public const string RsGeInvalidDocument = "RSGE_INVALID_DOCUMENT";
    public const string WaybillNotFound = "RSGE_WAYBILL_NOT_FOUND";
    public const string WaybillInvalidState = "RSGE_WAYBILL_INVALID_STATE";
    public const string FiscalDocumentNotFound = "RSGE_FISCAL_DOC_NOT_FOUND";

    // ── Licensing ────────────────────────────────────────────────
    public const string LicenseInvalid = "LICENSE_INVALID";
    public const string LicenseExpired = "LICENSE_EXPIRED";
    public const string LicenseMachineIdMismatch = "LICENSE_MACHINE_MISMATCH";

    // ── Files ────────────────────────────────────────────────────
    public const string FileNotFound = "FILE_NOT_FOUND";
    public const string FileTooLarge = "FILE_TOO_LARGE";
    public const string FileTypeNotAllowed = "FILE_TYPE_NOT_ALLOWED";

    // ── Import/Export ────────────────────────────────────────────
    public const string ImportValidationFailed = "IMPORT_VALIDATION_FAILED";
    public const string ImportFormatUnsupported = "IMPORT_FORMAT_UNSUPPORTED";
    public const string ExportFailed = "EXPORT_FAILED";

    // ── Webhooks ─────────────────────────────────────────────────
    public const string WebhookNotFound = "WEBHOOK_NOT_FOUND";
    public const string WebhookUrlInvalid = "WEBHOOK_URL_INVALID";
    public const string WebhookDeliveryFailed = "WEBHOOK_DELIVERY_FAILED";
}
