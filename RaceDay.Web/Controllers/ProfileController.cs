using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Security;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[SessionAuthorize]
public class ProfileController : Controller
{
    private readonly ApiClient api;

    public ProfileController(ApiClient api)
    {
        this.api = api;
    }

    public async Task<IActionResult> Index()
    {
        var result = await api.GetAsync<UserProfile>("api/profile");

        if (!result.Succeeded || result.Value is null)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction("Index", "Home");
        }

        var profile = result.Value;
        return View(new ProfileFormViewModel
        {
            FullName = profile.FullName,
            Email = profile.Email,
            Role = profile.Role,
            ProfilePictureUrl = profile.ProfilePictureUrl
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ProfileFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var saved = await api.PutAsync<UserProfile>("api/profile", new
        {
            form.FullName,
            form.ProfilePictureUrl
        });

        if (!saved.Succeeded)
        {
            ModelState.AddModelError(string.Empty, saved.ErrorMessage!);
            return View(form);
        }

        if (form.PictureFile is not null)
        {
            var uploaded = await api.PostFileAsync<UserProfile>(
                "api/profile/picture", "pictureFile", form.PictureFile);

            if (!uploaded.Succeeded)
            {
                ModelState.AddModelError(string.Empty, uploaded.ErrorMessage!);
                return View(form);
            }
        }

        // The navigation greets the user by name, so the session copy has to
        // follow the change.
        HttpContext.Session.SetString(SessionKeys.FullName, form.FullName);

        TempData["Message"] = "Profile saved.";
        return RedirectToAction(nameof(Index));
    }
}
