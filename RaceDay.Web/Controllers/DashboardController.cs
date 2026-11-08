using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Security;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[SessionAuthorize(RoleNames.Organiser)]
public class DashboardController : Controller
{
    private readonly ApiClient api;

    public DashboardController(ApiClient api)
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
}
