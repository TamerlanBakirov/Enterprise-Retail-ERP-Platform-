using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GeorgiaERP.Api.Middleware;

public sealed class PermissionAuthorizationFilter : IAsyncAuthorizationFilter
{
    private static readonly Dictionary<string, string> Modules = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Users"] = "identity", ["Products"] = "products", ["Pricing"] = "products",
        ["Inventory"] = "inventory", ["Pos"] = "pos", ["Procurement"] = "procurement",
        ["Compliance"] = "compliance", ["Finance"] = "finance", ["Customers"] = "crm",
        ["Organization"] = "organization", ["Reports"] = "reports"
    };

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var endpoint = context.HttpContext.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() is not null ||
            endpoint?.Metadata.GetMetadata<IAuthorizeData>() is null ||
            context.HttpContext.User.IsInRole("super_admin"))
            return Task.CompletedTask;

        if (context.ActionDescriptor is not ControllerActionDescriptor action ||
            !Modules.TryGetValue(action.ControllerName, out var module))
            return Task.CompletedTask;

        var permissionAction = ResolveAction(context.HttpContext.Request.Method, action.ActionName);
        var accepted = new[] { $"{module}:{permissionAction}:*", $"{module}:manage:*" };
        if (!context.HttpContext.User.Claims.Any(c => c.Type == "permissions" && accepted.Contains(c.Value, StringComparer.OrdinalIgnoreCase)))
            context.Result = new ForbidResult();

        return Task.CompletedTask;
    }

    private static string ResolveAction(string method, string actionName) => method switch
    {
        "GET" or "HEAD" => "read",
        "DELETE" => "delete",
        "PUT" or "PATCH" => "update",
        "POST" when actionName.StartsWith("Create", StringComparison.OrdinalIgnoreCase) => "create",
        _ => "manage"
    };
}
