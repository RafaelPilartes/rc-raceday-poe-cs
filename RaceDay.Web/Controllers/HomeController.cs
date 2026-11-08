using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Security;

namespace RaceDay.Web.Controllers;

public class HomeController : Controller
{
    [SessionAuthorize]
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Error()
    {
        return View();
    }
}
