using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using StudioTechBI.AgentHostApplication.Models;

namespace StudioTechBI.AgentHostAPI.Filters;

/// <summary>
/// Action filter that enforces the X-Admin-Key header against AdminSettings:AdminApiKey.
/// When AdminApiKey is empty (dev default) the check is bypassed to allow open Swagger testing.
/// In production AdminApiKey MUST be set via environment variable or Azure Key Vault.
/// </summary>
public sealed class AdminAuthFilter(IOptions<AdminSettings> adminSettings) : IActionFilter
{
    private const string AdminKeyHeader = "X-Admin-Key";

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var requiredKey = adminSettings.Value.AdminApiKey;
        if (string.IsNullOrEmpty(requiredKey)) return; // dev open mode

        var provided = context.HttpContext.Request.Headers[AdminKeyHeader].FirstOrDefault();
        if (provided != requiredKey)
        {
            context.Result = new ObjectResult(new
            {
                status = 403,
                title = "Forbidden",
                detail = "A valid X-Admin-Key header is required to access admin endpoints."
            })
            { StatusCode = StatusCodes.Status403Forbidden };
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
