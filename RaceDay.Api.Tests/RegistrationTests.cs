using Microsoft.AspNetCore.Http;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Tests.Support;

namespace RaceDay.Api.Tests;

public class RegistrationTests
{
    private static RegisterRequest ValidRequest(string role = RoleNames.Participant)
    {
        return new RegisterRequest
        {
            FullName = "Sipho Dlamini",
            Email = "sipho.dlamini@raceday.co.za",
            Password = TestHarness.KnownPassword,
            Role = role
        };
    }

    [Fact]
    public async Task Registering_creates_the_account_under_the_chosen_role()
    {
        using var db = TestHarness.NewDatabase();
        var controller = new AuthController(db).NotSignedIn();

        var response = await controller.Register(ValidRequest(RoleNames.Organiser));

        Assert.Equal(StatusCodes.Status201Created, TestHarness.StatusCodeOf(response));
        var created = TestHarness.BodyOf<RegisteredUserResponse>(response.Result);
        Assert.Equal(RoleNames.Organiser, created.Role);

        var saved = db.Users.Single();
        Assert.Equal("sipho.dlamini@raceday.co.za", saved.Email);
        Assert.Equal(created.UserId, saved.UserId);
    }

    [Fact]
    public async Task Registering_stores_a_hash_and_never_the_password_itself()
    {
        using var db = TestHarness.NewDatabase();
        var controller = new AuthController(db).NotSignedIn();

        await controller.Register(ValidRequest());

        var saved = db.Users.Single();
        Assert.NotEqual(TestHarness.KnownPassword, saved.PasswordHash);
        Assert.StartsWith("$2", saved.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify(TestHarness.KnownPassword, saved.PasswordHash));
    }

    [Fact]
    public async Task Registering_lower_cases_the_email_so_the_same_address_cannot_be_reused()
    {
        using var db = TestHarness.NewDatabase();
        var controller = new AuthController(db).NotSignedIn();

        var first = ValidRequest();
        first.Email = "Sipho.Dlamini@RaceDay.co.za";
        await controller.Register(first);

        var second = ValidRequest();
        second.Email = "sipho.dlamini@raceday.co.za";
        var response = await controller.Register(second);

        Assert.Equal(StatusCodes.Status409Conflict, TestHarness.StatusCodeOf(response));
        Assert.Single(db.Users);
    }

    [Fact]
    public async Task Registering_with_a_role_that_does_not_exist_is_rejected()
    {
        using var db = TestHarness.NewDatabase();
        var controller = new AuthController(db).NotSignedIn();

        var response = await controller.Register(ValidRequest("Administrator"));

        Assert.Equal(StatusCodes.Status400BadRequest, TestHarness.StatusCodeOf(response));
        Assert.Empty(db.Users);
    }

    [Theory]
    [InlineData("organiser")]
    [InlineData("PARTICIPANT")]
    public async Task Role_names_are_accepted_whatever_the_casing(string role)
    {
        using var db = TestHarness.NewDatabase();
        var controller = new AuthController(db).NotSignedIn();

        var response = await controller.Register(ValidRequest(role));

        Assert.Equal(StatusCodes.Status201Created, TestHarness.StatusCodeOf(response));
    }
}
