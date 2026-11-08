using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDayWeb.Infrastructure;

/// <summary>
/// Protects a page. A guest is sent to the login page and comes back after logging in. A logged-in user with
/// a different role is sent to Access Denied. Leave Role empty to allow any logged-in user.
/// </summary>
/// <remarks>
/// This only controls navigation in the MVC app. The API checks the role again on every call, so hiding a page
/// here is never the only protection.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireRoleAttribute(string? role = null) : Attribute, IAuthorizationFilter
{
    public string? Role { get; } = role;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.Session.CurrentUser();
        if (user is null)
        {
            var request = context.HttpContext.Request;
            var returnUrl = request.Method == HttpMethods.Get ? $"{request.Path}{request.QueryString}" : null;
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
            return;
        }

        if (Role is not null && user.Role != Role)
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }
}
