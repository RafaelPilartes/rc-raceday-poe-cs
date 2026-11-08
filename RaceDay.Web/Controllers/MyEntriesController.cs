using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Security;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[SessionAuthorize(RoleNames.Participant)]
public class MyEntriesController : Controller
{
    private readonly ApiClient api;

    public MyEntriesController(ApiClient api)
    {
        this.api = api;
    }

    public async Task<IActionResult> Index()
    {
        var result = await api.GetAsync<List<MyEntry>>("api/enrolments/my");

        if (!result.Succeeded)
        {
            TempData["Error"] = result.ErrorMessage;
            return View(new List<MyEntry>());
        }

        return View(result.Value ?? new List<MyEntry>());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enrol(int eventId, int selectedCategoryId)
    {
        if (selectedCategoryId == 0)
        {
            TempData["Error"] = "Choose a category before entering.";
            return RedirectToAction("Details", "Events", new { id = eventId });
        }

        var result = await api.PostAsync<MyEntry>(
            $"api/events/{eventId}/enrolments", new { CategoryId = selectedCategoryId });

        if (!result.Succeeded)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction("Details", "Events", new { id = eventId });
        }

        TempData["Message"] = $"You are entered for {result.Value?.EventName}.";
        return RedirectToAction(nameof(Index));
    }
}
