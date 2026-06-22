using System.Security.Claims;
using GeorgiaERP.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace GeorgiaERP.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    protected Guid CurrentUserId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) && id != Guid.Empty
            ? id
            : throw new UnauthorizedAccessException("User identity claim is missing or invalid.");

    protected Guid? CurrentCompanyId =>
        Guid.TryParse(User.FindFirstValue("company_id"), out var id)
            ? id
            : null;

    protected string? CurrentIpAddress =>
        HttpContext.Connection.RemoteIpAddress?.ToString();

    /// <summary>
    /// Converts a <see cref="Result"/> to an appropriate <see cref="IActionResult"/>,
    /// mapping error codes to HTTP status codes for consistent API responses.
    /// </summary>
    protected IActionResult ToActionResult(Result result)
    {
        if (result.IsSuccess)
            return Ok();

        return MapErrorToResponse(result);
    }

    /// <summary>
    /// Converts a <see cref="Result{T}"/> to an appropriate <see cref="IActionResult"/>,
    /// returning the value on success or the error details on failure.
    /// </summary>
    protected IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        return MapErrorToResponse(result);
    }

    /// <summary>
    /// Maps Result error codes to HTTP status codes.
    /// Uses the ErrorCodes constants for consistent mapping across the application.
    /// </summary>
    private IActionResult MapErrorToResponse(Result result)
    {
        return result.ErrorCode switch
        {
            ErrorCodes.NotFound => NotFound(new { error = result.Error, errorCode = result.ErrorCode }),
            ErrorCodes.ValidationError => BadRequest(new { error = result.Error, errorCode = result.ErrorCode, errors = result.Errors }),
            ErrorCodes.Unauthorized or ErrorCodes.InvalidCredentials => Unauthorized(new { error = result.Error, errorCode = result.ErrorCode }),
            ErrorCodes.Forbidden => StatusCode(403, new { error = result.Error, errorCode = result.ErrorCode }),
            ErrorCodes.Conflict => Conflict(new { error = result.Error, errorCode = result.ErrorCode }),

            // Map specific domain error codes to appropriate HTTP status codes
            ErrorCodes.ProductNotFound or ErrorCodes.CategoryNotFound or ErrorCodes.UserNotFound
                or ErrorCodes.WarehouseNotFound or ErrorCodes.SupplierNotFound
                or ErrorCodes.PurchaseOrderNotFound or ErrorCodes.CustomerNotFound
                or ErrorCodes.AccountNotFound or ErrorCodes.PriceListNotFound
                or ErrorCodes.WaybillNotFound or ErrorCodes.FileNotFound
                or ErrorCodes.WebhookNotFound or ErrorCodes.StockLevelNotFound
                or ErrorCodes.TransferOrderNotFound or ErrorCodes.StockCountNotFound
                or ErrorCodes.PosSessionNotFound or ErrorCodes.PosTransactionNotFound
                or ErrorCodes.JournalEntryNotFound or ErrorCodes.BankAccountNotFound
                or ErrorCodes.PromotionNotFound or ErrorCodes.FiscalDocumentNotFound
                or ErrorCodes.ReceivingOrderNotFound or ErrorCodes.ShippingOrderNotFound
                or ErrorCodes.WarehouseLocationNotFound or ErrorCodes.GoodsReceiptNotFound
                => NotFound(new { error = result.Error, errorCode = result.ErrorCode }),

            ErrorCodes.ProductSkuExists or ErrorCodes.UsernameTaken or ErrorCodes.EmailTaken
                or ErrorCodes.CustomerNumberExists or ErrorCodes.AccountCodeExists
                or ErrorCodes.PromotionOverlap
                => Conflict(new { error = result.Error, errorCode = result.ErrorCode }),

            ErrorCodes.InsufficientStock or ErrorCodes.NegativeQuantity
                or ErrorCodes.TransferOrderInvalidState or ErrorCodes.StockCountInvalidState
                or ErrorCodes.PurchaseOrderInvalidState or ErrorCodes.WaybillInvalidState
                or ErrorCodes.JournalEntryUnbalanced or ErrorCodes.JournalEntryAlreadyPosted
                or ErrorCodes.PosSessionClosed or ErrorCodes.PosTransactionAlreadyVoided
                or ErrorCodes.PosPaymentInsufficient
                => BadRequest(new { error = result.Error, errorCode = result.ErrorCode }),

            ErrorCodes.AccountLocked or ErrorCodes.AccountDisabled
                or ErrorCodes.TokenExpired or ErrorCodes.TokenInvalid
                or ErrorCodes.RefreshTokenExpired or ErrorCodes.RefreshTokenRevoked
                => Unauthorized(new { error = result.Error, errorCode = result.ErrorCode }),

            ErrorCodes.LicenseInvalid or ErrorCodes.LicenseExpired or ErrorCodes.LicenseMachineIdMismatch
                => StatusCode(403, new { error = result.Error, errorCode = result.ErrorCode }),

            ErrorCodes.RateLimitExceeded
                => StatusCode(429, new { error = result.Error, errorCode = result.ErrorCode }),

            _ => BadRequest(new { error = result.Error, errorCode = result.ErrorCode })
        };
    }
}
