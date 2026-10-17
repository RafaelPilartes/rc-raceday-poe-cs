using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Tests.Support;

/// <summary>
/// Shared setup for the controller tests: a throwaway database per test, a few
/// builders for the rows most tests need, and the plumbing that puts a logged-in
/// session on a controller.
/// </summary>
internal static class TestHarness
{
    public const string KnownPassword = "Run10km!2026";

    /// <summary>
    /// A fresh in-memory database for a single test, already carrying the two
    /// seeded roles. The random name keeps tests from sharing state when xUnit
    /// runs them in parallel.
    /// </summary>
    public static RaceDayDbContext NewDatabase()
    {
        var options = new DbContextOptionsBuilder<RaceDayDbContext>()
            .UseInMemoryDatabase($"raceday-tests-{Guid.NewGuid()}")
            .Options;

        var db = new RaceDayDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    public static User AddUser(
        this RaceDayDbContext db,
        string roleName,
        string email,
        string fullName = "Test Runner")
    {
        var role = db.Roles.Single(r => r.RoleName == roleName);

        var user = new User
        {
            RoleId = role.RoleId,
            FullName = fullName,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(KnownPassword),
            CreatedAt = DateTime.Now
        };

        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    public static Event AddEvent(
        this RaceDayDbContext db,
        int organiserId,
        string eventName = "Cape Town Coastal 10K",
        DateTime? eventDate = null)
    {
        var raceEvent = new Event
        {
            OrganiserId = organiserId,
            EventName = eventName,
            EventDate = eventDate ?? DateTime.Now.AddMonths(2),
            Location = "Sea Point Promenade, Cape Town",
            DistanceKm = 10m,
            EventType = EventTypes.Run,
            CreatedAt = DateTime.Now
        };

        db.Events.Add(raceEvent);
        db.SaveChanges();
        return raceEvent;
    }

    public static Category AddCategory(
        this RaceDayDbContext db,
        int eventId,
        string categoryName = "Open 20-39")
    {
        var category = new Category
        {
            EventId = eventId,
            CategoryName = categoryName,
            MinAge = 20,
            MaxAge = 39,
            DistanceKm = 10m
        };

        db.Categories.Add(category);
        db.SaveChanges();
        return category;
    }

    public static Enrolment AddEnrolment(
        this RaceDayDbContext db,
        int participantId,
        int eventId,
        int categoryId,
        string status = EnrolmentStatuses.Confirmed)
    {
        var enrolment = new Enrolment
        {
            ParticipantId = participantId,
            EventId = eventId,
            CategoryId = categoryId,
            EnrolmentDate = DateTime.Now,
            Status = status
        };

        db.Enrolments.Add(enrolment);
        db.SaveChanges();
        return enrolment;
    }

    /// <summary>Hands the controller a session that is logged in as this user.</summary>
    public static TController SignedInAs<TController>(this TController controller, User user)
        where TController : ControllerBase
    {
        var httpContext = new DefaultHttpContext { Session = new FakeSession() };
        httpContext.Session.StartUserSession(user.UserId, user.Role.RoleName);
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    /// <summary>Hands the controller an empty session, as an anonymous caller would.</summary>
    public static TController NotSignedIn<TController>(this TController controller)
        where TController : ControllerBase
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { Session = new FakeSession() }
        };
        return controller;
    }

    public static int StatusCodeOf<TValue>(ActionResult<TValue> actionResult)
    {
        return StatusCodeOf(actionResult.Result);
    }

    public static int StatusCodeOf(IActionResult? result)
    {
        return result switch
        {
            ObjectResult objectResult => objectResult.StatusCode ?? StatusCodes.Status200OK,
            StatusCodeResult statusCodeResult => statusCodeResult.StatusCode,
            null => throw new InvalidOperationException(
                "The action returned a bare value instead of an IActionResult."),
            _ => throw new InvalidOperationException($"Unhandled result type {result.GetType().Name}.")
        };
    }

    /// <summary>
    /// The response body, checked to be the type the test expects. Callers pass
    /// actionResult.Result rather than the ActionResult itself, because an
    /// overload taking both cannot be resolved without ambiguity.
    /// </summary>
    public static TBody BodyOf<TBody>(IActionResult? result)
    {
        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        return Assert.IsType<TBody>(objectResult.Value);
    }
}
