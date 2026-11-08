using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Security;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[SessionAuthorize(RoleNames.Participant)]
public class MyResultsController : Controller
{
    private readonly ApiClient api;

    public MyResultsController(ApiClient api)
    {
        this.api = api;
    }

    public async Task<IActionResult> Index()
    {
        var result = await api.GetAsync<List<MyResult>>("api/results/my");

        if (!result.Succeeded)
        {
            TempData["Error"] = result.ErrorMessage;
            return View(new List<MyResult>());
        }

        return View(result.Value ?? new List<MyResult>());
    }
}
