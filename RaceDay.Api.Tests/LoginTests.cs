using Microsoft.AspNetCore.Http;
using RaceDay.Api.Controllers;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;
using RaceDay.Api.Tests.Support;

namespace RaceDay.Api.Tests;

public class LoginTests
{
    private const string OrganiserEmail = "thabo.nkosi@raceday.co.za";

    private static AuthController ControllerWithOrganiser(RaceDayDbContext db)
    {
        db.AddUser(RoleNames.Organiser, OrganiserEmail, "Thabo Nkosi");
        return new AuthController(db).NotSignedIn();
    }

    [Fact]
    public async Task Correct_credentials_start_a_session_carrying_the_user_and_role()
    {
        using var db = TestHarness.NewDatabase();
        var controller = ControllerWithOrganiser(db);

        var response = await controller.Login(new LoginRequest
        {
            Email = OrganiserEmail,
            Password = TestHarness.KnownPassword
        });

        Assert.Equal(StatusCodes.Status200OK, TestHarness.StatusCodeOf(response));

        var loggedIn = TestHarness.BodyOf<LoggedInUserResponse>(response.Result);
        Assert.Equal(RoleNames.Organiser, loggedIn.Role);

        var session = controller.HttpContext.Session;
        Assert.Equal(loggedIn.UserId, session.GetUserId());
        Assert.Equal(RoleNames.Organiser, session.GetRoleName());
    }

    [Fact]
    public async Task The_email_can_be_typed_in_any_casing()
    {
        using var db = TestHarness.NewDatabase();
        var controller = ControllerWithOrganiser(db);

        var response = await controller.Login(new LoginRequest
        {
            Email = "Thabo.Nkosi@RaceDay.co.za",
            Password = TestHarness.KnownPassword
        });

        Assert.Equal(StatusCodes.Status200OK, TestHarness.StatusCodeOf(response));
    }

    [Fact]
    public async Task A_wrong_password_is_rejected_and_opens_no_session()
    {
        using var db = TestHarness.NewDatabase();
        var controller = ControllerWithOrganiser(db);

        var response = await controller.Login(new LoginRequest
        {
            Email = OrganiserEmail,
            Password = "NotTheRightOne1"
        });

        Assert.Equal(StatusCodes.Status401Unauthorized, TestHarness.StatusCodeOf(response));
        Assert.Null(controller.HttpContext.Session.GetUserId());
    }

    [Fact]
    public async Task An_email_with_no_account_is_rejected()
    {
        using var db = TestHarness.NewDatabase();
        var controller = ControllerWithOrganiser(db);

        var response = await controller.Login(new LoginRequest
        {
            Email = "nobody@raceday.co.za",
            Password = TestHarness.KnownPassword
        });

        Assert.Equal(StatusCodes.Status401Unauthorized, TestHarness.StatusCodeOf(response));
        Assert.Null(controller.HttpContext.Session.GetUserId());
    }

    [Fact]
    public async Task A_missing_account_and_a_wrong_password_give_the_same_answer()
    {
        using var db = TestHarness.NewDatabase();
        var controller = ControllerWithOrganiser(db);

        var wrongPassword = await controller.Login(new LoginRequest
        {
            Email = OrganiserEmail,
            Password = "NotTheRightOne1"
        });

        var unknownEmail = await controller.Login(new LoginRequest
        {
            Email = "nobody@raceday.co.za",
            Password = TestHarness.KnownPassword
        });

        Assert.Equal(
            TestHarness.BodyOf<MessageResponse>(wrongPassword.Result).Message,
            TestHarness.BodyOf<MessageResponse>(unknownEmail.Result).Message);
    }

    [Fact]
    public void Logging_out_empties_the_session()
    {
        using var db = TestHarness.NewDatabase();
        var organiser = db.AddUser(RoleNames.Organiser, OrganiserEmail, "Thabo Nkosi");
        var controller = new AuthController(db).SignedInAs(organiser);

        var result = controller.Logout();

        Assert.Equal(StatusCodes.Status200OK, TestHarness.StatusCodeOf(result));
        Assert.Null(controller.HttpContext.Session.GetUserId());
        Assert.Null(controller.HttpContext.Session.GetRoleName());
    }
}
