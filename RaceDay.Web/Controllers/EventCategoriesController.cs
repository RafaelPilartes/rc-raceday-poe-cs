using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Security;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[SessionAuthorize(RoleNames.Organiser)]
public class EventCategoriesController : Controller
{
    private readonly ApiClient api;

    public EventCategoriesController(ApiClient api)
    {
        this.api = api;
    }

    public async Task<IActionResult> Index(int eventId)
    {
        var page = await LoadPageAsync(eventId);

        if (page is null)
        {
            return RedirectToAction("Index", "MyEvents");
        }

        return View(page);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int eventId, CategoryFormViewModel form)
    {
        if (ModelState.IsValid)
        {
            var created = await api.PostAsync<EventCategory>(
                $"api/events/{eventId}/categories", BuildCategoryBody(form));

            if (created.Succeeded)
            {
                TempData["Message"] = $"{form.CategoryName} added.";
                return RedirectToAction(nameof(Index), new { eventId });
            }

            ModelState.AddModelError(string.Empty, created.ErrorMessage!);
        }

        var page = await LoadPageAsync(eventId);
        if (page is null)
        {
            return RedirectToAction("Index", "MyEvents");
        }

        page.NewCategory = form;
        return View(nameof(Index), page);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int eventId, int id)
    {
        var categories = await api.GetAsync<List<EventCategory>>($"api/events/{eventId}/categories");

        var category = categories.Value?.FirstOrDefault(entry => entry.CategoryId == id);
        if (category is null)
        {
            TempData["Error"] = categories.ErrorMessage ?? "That category no longer exists.";
            return RedirectToAction(nameof(Index), new { eventId });
        }

        return View(new CategoryFormViewModel
        {
            CategoryId = category.CategoryId,
            EventId = category.EventId,
            CategoryName = category.CategoryName,
            MinAge = category.MinAge,
            MaxAge = category.MaxAge,
            DistanceKm = category.DistanceKm
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CategoryFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var saved = await api.PutAsync<EventCategory>(
            $"api/categories/{form.CategoryId}", BuildCategoryBody(form));

        if (!saved.Succeeded)
        {
            ModelState.AddModelError(string.Empty, saved.ErrorMessage!);
            return View(form);
        }

        TempData["Message"] = "Category updated.";
        return RedirectToAction(nameof(Index), new { eventId = form.EventId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int eventId, int id)
    {
        var result = await api.DeleteAsync($"api/categories/{id}");

        if (result.Succeeded)
        {
            TempData["Message"] = "Category removed.";
        }
        else
        {
            TempData["Error"] = result.ErrorMessage;
        }

        return RedirectToAction(nameof(Index), new { eventId });
    }

    private async Task<EventCategoriesViewModel?> LoadPageAsync(int eventId)
    {
        var raceEvent = await api.GetAsync<EventDetail>($"api/events/{eventId}");
        if (!raceEvent.Succeeded || raceEvent.Value is null)
        {
            TempData["Error"] = raceEvent.ErrorMessage;
            return null;
        }

        var categories = await api.GetAsync<List<EventCategory>>($"api/events/{eventId}/categories");

        return new EventCategoriesViewModel
        {
            Event = raceEvent.Value,
            Categories = categories.Value ?? new List<EventCategory>(),
            NewCategory = new CategoryFormViewModel { EventId = eventId }
        };
    }

    private static object BuildCategoryBody(CategoryFormViewModel form)
    {
        return new
        {
            form.CategoryName,
            form.MinAge,
            form.MaxAge,
            form.DistanceKm
        };
    }
}
