using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Security;

namespace RaceDay.Web.Controllers;

public class HomeController : Controller
{
    [SessionAuthorize]
    public IActionResult Index()
    {
        if (HttpContext.Session.GetRoleName() == RoleNames.Organiser)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return View();
    }

    public IActionResult Error()
    {
        return View();
    }
}
