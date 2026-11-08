using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Web.Security;

/// <summary>
/// Sends anyone without a session to the login page, and anyone holding the
/// wrong role to the access denied page. Pass no roles to mean "any signed-in
/// user". This mirrors the filter the API uses, so a page and the endpoint
/// behind it are protected by the same rule.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] allowedRoles;

    public SessionAuthorizeAttribute(params string[] allowedRoles)
    {
        this.allowedRoles = allowedRoles;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;

        if (session.GetUserId() is null)
        {
            var returnUrl = context.HttpContext.Request.Path.ToString();
            context.Result = new RedirectToActionResult(
                "Login", "Account", new { returnUrl });
            return;
        }

        if (allowedRoles.Length == 0)
        {
            return;
        }

        var roleName = session.GetRoleName();
        if (roleName is null || !allowedRoles.Contains(roleName, StringComparer.OrdinalIgnoreCase))
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }
}
