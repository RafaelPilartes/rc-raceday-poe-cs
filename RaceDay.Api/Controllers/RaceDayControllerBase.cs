using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

/// <summary>
/// Shared base for every controller that sits behind [SessionAuthorize]. By the
/// time an action runs the filter has already proved a session exists, so the
/// user id can be read without a null check on each use.
/// </summary>
public abstract class RaceDayControllerBase : ControllerBase
{
    protected int CurrentUserId =>
        HttpContext.Session.GetUserId()
        ?? throw new InvalidOperationException(
            "No user id in session. The action is missing the SessionAuthorize filter.");
}
