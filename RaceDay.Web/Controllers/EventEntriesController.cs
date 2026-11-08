using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Security;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[SessionAuthorize(RoleNames.Organiser)]
public class EventEntriesController : RaceDayController
{
    private readonly ApiClient api;

    public EventEntriesController(ApiClient api)
    {
        this.api = api;
    }

    public async Task<IActionResult> Index(int eventId)
    {
        var raceEvent = await api.GetAsync<EventDetail>($"api/events/{eventId}");
        if (!raceEvent.Succeeded || raceEvent.Value is null)
        {
            TempData["Error"] = raceEvent.ErrorMessage;
            return RedirectToAction("Index", "MyEvents");
        }

        if (raceEvent.Value.OrganiserId != CurrentUserId)
        {
            TempData["Error"] = "That event belongs to another organiser.";
            return RedirectToAction("Index", "MyEvents");
        }

        var entries = await api.GetAsync<List<EventEntry>>($"api/events/{eventId}/enrolments");
        if (!entries.Succeeded)
        {
            TempData["Error"] = entries.ErrorMessage;
        }

        ViewData["Event"] = raceEvent.Value;
        return View(entries.Value ?? new List<EventEntry>());
    }
}
