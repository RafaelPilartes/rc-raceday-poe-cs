using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Api.Dtos;

namespace RaceDay.Api.Security;

/// <summary>
/// Rejects a request unless the caller has an active session. Pass one or more
/// role names to also restrict the endpoint to those roles; with no roles it
/// simply means "any logged-in user".
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    public SessionAuthorizeAttribute(params string[] allowedRoles)
    {
        AllowedRoles = allowedRoles;
    }

    /// <summary>
    /// The roles this endpoint accepts. Empty means any logged-in user. Public
    /// so the tests can confirm each endpoint asks for the role the Part 1 plan
    /// says it should.
    /// </summary>
    public IReadOnlyList<string> AllowedRoles { get; }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;

        if (session.GetUserId() is null)
        {
            context.Result = new UnauthorizedObjectResult(
                new MessageResponse("Log in first; this endpoint needs an active session."));
            return;
        }

        if (AllowedRoles.Count == 0)
        {
            return;
        }

        var roleName = session.GetRoleName();
        if (roleName is null || !AllowedRoles.Contains(roleName, StringComparer.OrdinalIgnoreCase))
        {
            context.Result = new ObjectResult(new MessageResponse(
                $"This endpoint is restricted to the {string.Join(" and ", AllowedRoles)} role."))
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
