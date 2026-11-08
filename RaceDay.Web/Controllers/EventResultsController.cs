using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Security;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[SessionAuthorize(RoleNames.Organiser)]
public class EventResultsController : Controller
{
    private readonly ApiClient api;

    public EventResultsController(ApiClient api)
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

        var entries = await api.GetAsync<List<EventEntry>>($"api/events/{eventId}/enrolments");
        if (!entries.Succeeded)
        {
            TempData["Error"] = entries.ErrorMessage;
        }

        ViewData["Event"] = raceEvent.Value;
        return View(entries.Value ?? new List<EventEntry>());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(CaptureResultViewModel form)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Write the finish time as HH:MM:SS and give a position of 1 or more.";
            return RedirectToAction(nameof(Index), new { eventId = form.EventId });
        }

        var path = $"api/enrolments/{form.EnrolmentId}/result";
        var body = new { form.FinishTime, form.FinishingPosition };

        var result = form.AlreadyCaptured
            ? await api.PutAsync<object>(path, body)
            : await api.PostAsync<object>(path, body);

        if (result.Succeeded)
        {
            TempData["Message"] = form.AlreadyCaptured ? "Result corrected." : "Result published.";
        }
        else
        {
            TempData["Error"] = result.ErrorMessage;
        }

        return RedirectToAction(nameof(Index), new { eventId = form.EventId });
    }
}
