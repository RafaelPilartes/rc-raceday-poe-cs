using Microsoft.AspNetCore.Http;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Tests.Support;

namespace RaceDay.Api.Tests;

public class ProfileTests
{
    [Theory]
    [InlineData(RoleNames.Organiser)]
    [InlineData(RoleNames.Participant)]
    public async Task Either_role_can_read_their_own_profile(string roleName)
    {
        using var db = TestHarness.NewDatabase();
        var user = db.AddUser(roleName, "sipho.dlamini@raceday.co.za", "Sipho Dlamini");
        var controller = new ProfileController(db, new FakeBlobStorage()).SignedInAs(user);

        var response = await controller.GetMyProfile();

        Assert.Equal(StatusCodes.Status200OK, TestHarness.StatusCodeOf(response));

        var profile = TestHarness.BodyOf<ProfileResponse>(response.Result);
        Assert.Equal(user.UserId, profile.UserId);
        Assert.Equal("Sipho Dlamini", profile.FullName);
        Assert.Equal(roleName, profile.Role);
    }

    [Fact]
    public async Task A_profile_read_never_returns_the_password_hash()
    {
        using var db = TestHarness.NewDatabase();
        var user = db.AddUser(RoleNames.Participant, "sipho.dlamini@raceday.co.za");
        var controller = new ProfileController(db, new FakeBlobStorage()).SignedInAs(user);

        var response = await controller.GetMyProfile();

        var profile = TestHarness.BodyOf<ProfileResponse>(response.Result);
        Assert.DoesNotContain(
            nameof(User.PasswordHash),
            profile.GetType().GetProperties().Select(property => property.Name));
    }

    [Fact]
    public async Task Updating_changes_the_name_and_the_picture()
    {
        using var db = TestHarness.NewDatabase();
        var user = db.AddUser(RoleNames.Participant, "sipho.dlamini@raceday.co.za", "Sipho Dlamini");
        var controller = new ProfileController(db, new FakeBlobStorage()).SignedInAs(user);

        var response = await controller.UpdateMyProfile(new UpdateProfileRequest
        {
            FullName = "Sipho M. Dlamini",
            ProfilePictureUrl = "https://raceday.co.za/avatars/sipho.jpg"
        });

        Assert.Equal(StatusCodes.Status200OK, TestHarness.StatusCodeOf(response));

        var saved = db.Users.Single();
        Assert.Equal("Sipho M. Dlamini", saved.FullName);
        Assert.Equal("https://raceday.co.za/avatars/sipho.jpg", saved.ProfilePictureUrl);
    }

    [Fact]
    public async Task Sending_a_blank_picture_url_clears_the_stored_one()
    {
        using var db = TestHarness.NewDatabase();
        var user = db.AddUser(RoleNames.Participant, "sipho.dlamini@raceday.co.za");
        user.ProfilePictureUrl = "https://raceday.co.za/avatars/old.jpg";
        db.SaveChanges();
        var controller = new ProfileController(db, new FakeBlobStorage()).SignedInAs(user);

        await controller.UpdateMyProfile(new UpdateProfileRequest
        {
            FullName = "Sipho Dlamini",
            ProfilePictureUrl = "   "
        });

        Assert.Null(db.Users.Single().ProfilePictureUrl);
    }

    [Fact]
    public async Task An_update_cannot_move_the_account_to_another_email_or_role()
    {
        using var db = TestHarness.NewDatabase();
        var user = db.AddUser(RoleNames.Participant, "sipho.dlamini@raceday.co.za");
        var controller = new ProfileController(db, new FakeBlobStorage()).SignedInAs(user);

        await controller.UpdateMyProfile(new UpdateProfileRequest { FullName = "Renamed" });

        var saved = db.Users.Single();
        Assert.Equal("sipho.dlamini@raceday.co.za", saved.Email);
        Assert.Equal(RoleNames.Participant, saved.Role.RoleName);
    }

    [Fact]
    public async Task A_session_whose_account_has_been_removed_is_rejected()
    {
        using var db = TestHarness.NewDatabase();
        var user = db.AddUser(RoleNames.Participant, "sipho.dlamini@raceday.co.za");
        var controller = new ProfileController(db, new FakeBlobStorage()).SignedInAs(user);
        db.Users.Remove(user);
        db.SaveChanges();

        var response = await controller.GetMyProfile();

        Assert.Equal(StatusCodes.Status401Unauthorized, TestHarness.StatusCodeOf(response));
    }
}
