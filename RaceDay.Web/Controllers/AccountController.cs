using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Security;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class AccountController : Controller
{
    private readonly ApiClient api;

    public AccountController(ApiClient api)
    {
        this.api = api;
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel form)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var result = await api.PostAsync<RegisteredUser>("api/auth/register", new
        {
            form.FullName,
            form.Email,
            form.Password,
            form.Role
        });

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage!);
            return View(form);
        }

        TempData["Message"] = "Account created. Sign in to get started.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel form, string? returnUrl)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(form);
        }

        // Drop anything left over from a previous session before asking the API
        // for a new one, so a failed sign-in cannot leave the old user in place.
        HttpContext.Session.Clear();

        var result = await api.PostAsync<LoggedInUser>("api/auth/login", new
        {
            form.Email,
            form.Password
        });

        if (!result.Succeeded || result.Value is null)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Could not sign you in.");
            return View(form);
        }

        HttpContext.Session.StartUserSession(
            result.Value.UserId, result.Value.Role, result.Value.FullName);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [SessionAuthorize]
    public async Task<IActionResult> Logout()
    {
        await api.PostEmptyAsync("api/auth/logout");
        HttpContext.Session.Clear();

        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
