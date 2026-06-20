using System.Security.Claims;
using GeorgiaERP.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace GeorgiaERP.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    protected Guid CurrentUserId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : Guid.Empty;

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

        return result.ErrorCode switch
        {
            "NOT_FOUND" => NotFound(new { error = result.Error, errorCode = result.ErrorCode }),
            "VALIDATION_ERROR" => BadRequest(new { error = result.Error, errorCode = result.ErrorCode, errors = result.Errors }),
            _ => BadRequest(new { error = result.Error, errorCode = result.ErrorCode })
        };
    }

    /// <summary>
    /// Converts a <see cref="Result{T}"/> to an appropriate <see cref="IActionResult"/>,
    /// returning the value on success or the error details on failure.
    /// </summary>
    protected IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        return result.ErrorCode switch
        {
            "NOT_FOUND" => NotFound(new { error = result.Error, errorCode = result.ErrorCode }),
            "VALIDATION_ERROR" => BadRequest(new { error = result.Error, errorCode = result.ErrorCode, errors = result.Errors }),
            _ => BadRequest(new { error = result.Error, errorCode = result.ErrorCode })
        };
    }
}
