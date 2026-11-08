using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Security;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[SessionAuthorize(RoleNames.Organiser)]
public class MyEventsController : RaceDayController
{
    private readonly ApiClient api;

    public MyEventsController(ApiClient api)
    {
        this.api = api;
    }

    public async Task<IActionResult> Index()
    {
        var result = await api.GetAsync<List<OrganiserEvent>>("api/events/my");

        if (!result.Succeeded)
        {
            TempData["Error"] = result.ErrorMessage;
            return View(new List<OrganiserEvent>());
        }

        return View(result.Value ?? new List<OrganiserEvent>());
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new EventFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var created = await api.PostAsync<EventDetail>("api/events", BuildEventBody(form));
        if (!created.Succeeded || created.Value is null)
        {
            ModelState.AddModelError(string.Empty, created.ErrorMessage!);
            return View(form);
        }

        if (form.BannerFile is not null)
        {
            var uploaded = await UploadBannerAsync(created.Value.EventId, form.BannerFile);
            if (!uploaded.Succeeded)
            {
                TempData["Error"] = $"The event was created but the banner was not saved. {uploaded.ErrorMessage}";
                return RedirectToAction(nameof(Edit), new { id = created.Value.EventId });
            }
        }

        TempData["Message"] = $"{created.Value.EventName} is published.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await api.GetAsync<EventDetail>($"api/events/{id}");

        if (!result.Succeeded || result.Value is null)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }

        if (result.Value.OrganiserId != CurrentUserId)
        {
            TempData["Error"] = "That event belongs to another organiser.";
            return RedirectToAction("Index", "MyEvents");
        }

        var raceEvent = result.Value;
        return View(new EventFormViewModel
        {
            EventId = raceEvent.EventId,
            EventName = raceEvent.EventName,
            Description = raceEvent.Description,
            EventDate = raceEvent.EventDate,
            Location = raceEvent.Location,
            DistanceKm = raceEvent.DistanceKm,
            EventType = raceEvent.EventType,
            BannerImageUrl = raceEvent.BannerImageUrl
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EventFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var saved = await api.PutAsync<EventDetail>($"api/events/{form.EventId}", BuildEventBody(form));
        if (!saved.Succeeded)
        {
            ModelState.AddModelError(string.Empty, saved.ErrorMessage!);
            return View(form);
        }

        if (form.BannerFile is not null)
        {
            var uploaded = await UploadBannerAsync(form.EventId, form.BannerFile);
            if (!uploaded.Succeeded)
            {
                ModelState.AddModelError(string.Empty, uploaded.ErrorMessage!);
                return View(form);
            }
        }

        TempData["Message"] = "Event updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await api.GetAsync<EventDetail>($"api/events/{id}");

        if (!result.Succeeded || result.Value is null)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }

        if (result.Value.OrganiserId != CurrentUserId)
        {
            TempData["Error"] = "That event belongs to another organiser.";
            return RedirectToAction("Index", "MyEvents");
        }

        return View(result.Value);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmDelete(int id)
    {
        var result = await api.DeleteAsync($"api/events/{id}");

        if (result.Succeeded)
        {
            TempData["Message"] = "Event deleted.";
        }
        else
        {
            TempData["Error"] = result.ErrorMessage;
        }

        return RedirectToAction(nameof(Index));
    }

    private static object BuildEventBody(EventFormViewModel form)
    {
        return new
        {
            form.EventName,
            form.Description,
            form.EventDate,
            form.Location,
            form.DistanceKm,
            form.EventType,
            form.BannerImageUrl
        };
    }

    private Task<ApiResult<EventDetail>> UploadBannerAsync(int eventId, IFormFile bannerFile)
    {
        return api.PostFileAsync<EventDetail>($"api/events/{eventId}/banner", "bannerFile", bannerFile);
    }
}
