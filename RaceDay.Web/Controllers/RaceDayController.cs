using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Security;

namespace RaceDay.Web.Controllers;

/// <summary>
/// Base for the pages behind SessionAuthorize. The filter has already proved
/// there is a session by the time an action runs, so the user id can be read
/// without a null check.
/// </summary>
public abstract class RaceDayController : Controller
{
    protected int CurrentUserId =>
        HttpContext.Session.GetUserId()
        ?? throw new InvalidOperationException(
            "No user id in session. The page is missing the SessionAuthorize filter.");
}
