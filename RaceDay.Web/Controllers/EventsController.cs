using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Security;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[SessionAuthorize]
public class EventsController : Controller
{
    private readonly ApiClient api;

    public EventsController(ApiClient api)
    {
        this.api = api;
    }

    public async Task<IActionResult> Index(EventSearchViewModel search)
    {
        var result = await api.GetAsync<List<EventSummary>>($"api/events{search.ToQueryString()}");

        if (!result.Succeeded)
        {
            TempData["Error"] = result.ErrorMessage;
        }

        search.Events = result.Value ?? new List<EventSummary>();
        return View(search);
    }

    public async Task<IActionResult> Details(int id)
    {
        var raceEvent = await api.GetAsync<EventDetail>($"api/events/{id}");
        if (!raceEvent.Succeeded || raceEvent.Value is null)
        {
            TempData["Error"] = raceEvent.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }

        var categories = await api.GetAsync<List<EventCategory>>($"api/events/{id}/categories");

        return View(new EventDetailsViewModel
        {
            Event = raceEvent.Value,
            Categories = categories.Value ?? new List<EventCategory>()
        });
    }
}
