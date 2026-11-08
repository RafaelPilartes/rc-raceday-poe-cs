using Microsoft.AspNetCore.Http;
using RaceDay.Api.Controllers;
using RaceDay.Api.Models;
using RaceDay.Api.Services;
using RaceDay.Api.Tests.Support;

namespace RaceDay.Api.Tests;

public class ProfilePictureUploadTests
{
    [Theory]
    [InlineData(RoleNames.Organiser)]
    [InlineData(RoleNames.Participant)]
    public async Task Either_role_can_upload_their_own_picture(string roleName)
    {
        using var db = TestHarness.NewDatabase();
        var user = db.AddUser(roleName, "sipho.dlamini@raceday.co.za");
        var blobStorage = new FakeBlobStorage();
        var controller = new ProfileController(db, blobStorage).SignedInAs(user);

        var response = await controller.UploadProfilePicture(TestHarness.ImageFile("sipho.png", "image/png"));

        Assert.Equal(StatusCodes.Status200OK, TestHarness.StatusCodeOf(response));
        Assert.Equal("sipho.png", Assert.Single(blobStorage.UploadedFileNames));
        Assert.Equal(
            "https://fake.blob.core.windows.net/raceday-images/sipho.png",
            db.Users.Single().ProfilePictureUrl);
    }

    [Fact]
    public async Task The_picture_is_always_stored_against_the_logged_in_user()
    {
        using var db = TestHarness.NewDatabase();
        var user = db.AddUser(RoleNames.Participant, "sipho.dlamini@raceday.co.za");
        var somebodyElse = db.AddUser(RoleNames.Participant, "lerato.mokoena@raceday.co.za");
        var controller = new ProfileController(db, new FakeBlobStorage()).SignedInAs(user);

        await controller.UploadProfilePicture(TestHarness.ImageFile());

        Assert.NotNull(db.Users.Single(u => u.UserId == user.UserId).ProfilePictureUrl);
        Assert.Null(db.Users.Single(u => u.UserId == somebodyElse.UserId).ProfilePictureUrl);
    }

    [Fact]
    public async Task Sending_no_file_at_all_is_rejected()
    {
        using var db = TestHarness.NewDatabase();
        var user = db.AddUser(RoleNames.Participant, "sipho.dlamini@raceday.co.za");
        var controller = new ProfileController(db, new FakeBlobStorage()).SignedInAs(user);

        var response = await controller.UploadProfilePicture(null);

        Assert.Equal(StatusCodes.Status400BadRequest, TestHarness.StatusCodeOf(response));
    }

    [Fact]
    public async Task A_file_that_is_not_an_image_is_rejected()
    {
        using var db = TestHarness.NewDatabase();
        var user = db.AddUser(RoleNames.Participant, "sipho.dlamini@raceday.co.za");
        var blobStorage = new FakeBlobStorage();
        var controller = new ProfileController(db, blobStorage).SignedInAs(user);

        var response = await controller.UploadProfilePicture(
            TestHarness.ImageFile("notes.txt", "text/plain"));

        Assert.Equal(StatusCodes.Status400BadRequest, TestHarness.StatusCodeOf(response));
        Assert.Empty(blobStorage.UploadedFileNames);
    }

    [Fact]
    public async Task An_image_over_five_megabytes_is_rejected()
    {
        using var db = TestHarness.NewDatabase();
        var user = db.AddUser(RoleNames.Participant, "sipho.dlamini@raceday.co.za");
        var blobStorage = new FakeBlobStorage();
        var controller = new ProfileController(db, blobStorage).SignedInAs(user);

        var hugeFile = TestHarness.ImageFile(sizeInBytes: (int)ImageUpload.MaxSizeInBytes + 1);
        var response = await controller.UploadProfilePicture(hugeFile);

        Assert.Equal(StatusCodes.Status400BadRequest, TestHarness.StatusCodeOf(response));
        Assert.Empty(blobStorage.UploadedFileNames);
    }
}
