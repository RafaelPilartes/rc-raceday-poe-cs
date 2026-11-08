using Microsoft.AspNetCore.Http;
using RaceDay.Api.Controllers;
using RaceDay.Api.Models;
using RaceDay.Api.Services;
using RaceDay.Api.Tests.Support;

namespace RaceDay.Api.Tests;

public class BannerUploadTests
{
    [Fact]
    public async Task Organiser_can_upload_a_banner_for_their_own_event()
    {
        using var db = TestHarness.NewDatabase();
        var organiser = db.AddUser(RoleNames.Organiser, "thabo.nkosi@raceday.co.za");
        var raceEvent = db.AddEvent(organiser.UserId);
        var blobStorage = new FakeBlobStorage();
        var controller = new EventsController(db, blobStorage).SignedInAs(organiser);

        var response = await controller.UploadBanner(raceEvent.EventId, TestHarness.ImageFile());

        Assert.Equal(StatusCodes.Status200OK, TestHarness.StatusCodeOf(response));
        Assert.Equal("banner.jpg", Assert.Single(blobStorage.UploadedFileNames));

        var saved = db.Events.Single();
        Assert.Equal(
            "https://fake.blob.core.windows.net/raceday-images/banner.jpg",
            saved.BannerImageUrl);
    }

    [Fact]
    public async Task An_organiser_cannot_change_the_banner_on_somebody_elses_event()
    {
        using var db = TestHarness.NewDatabase();
        var owner = db.AddUser(RoleNames.Organiser, "thabo.nkosi@raceday.co.za");
        var otherOrganiser = db.AddUser(RoleNames.Organiser, "amanda.vdm@raceday.co.za");
        var raceEvent = db.AddEvent(owner.UserId);
        var blobStorage = new FakeBlobStorage();
        var controller = new EventsController(db, blobStorage).SignedInAs(otherOrganiser);

        var response = await controller.UploadBanner(raceEvent.EventId, TestHarness.ImageFile());

        Assert.Equal(StatusCodes.Status403Forbidden, TestHarness.StatusCodeOf(response));
        Assert.Empty(blobStorage.UploadedFileNames);
        Assert.Null(db.Events.Single().BannerImageUrl);
    }

    [Fact]
    public async Task Uploading_to_an_event_that_does_not_exist_gives_404()
    {
        using var db = TestHarness.NewDatabase();
        var organiser = db.AddUser(RoleNames.Organiser, "thabo.nkosi@raceday.co.za");
        var controller = new EventsController(db, new FakeBlobStorage()).SignedInAs(organiser);

        var response = await controller.UploadBanner(4321, TestHarness.ImageFile());

        Assert.Equal(StatusCodes.Status404NotFound, TestHarness.StatusCodeOf(response));
    }

    [Fact]
    public async Task Sending_no_file_at_all_is_rejected()
    {
        using var db = TestHarness.NewDatabase();
        var organiser = db.AddUser(RoleNames.Organiser, "thabo.nkosi@raceday.co.za");
        var raceEvent = db.AddEvent(organiser.UserId);
        var controller = new EventsController(db, new FakeBlobStorage()).SignedInAs(organiser);

        var response = await controller.UploadBanner(raceEvent.EventId, null);

        Assert.Equal(StatusCodes.Status400BadRequest, TestHarness.StatusCodeOf(response));
    }

    [Fact]
    public async Task A_file_that_is_not_an_image_is_rejected()
    {
        using var db = TestHarness.NewDatabase();
        var organiser = db.AddUser(RoleNames.Organiser, "thabo.nkosi@raceday.co.za");
        var raceEvent = db.AddEvent(organiser.UserId);
        var blobStorage = new FakeBlobStorage();
        var controller = new EventsController(db, blobStorage).SignedInAs(organiser);

        var pdfFile = TestHarness.ImageFile("entries.pdf", "application/pdf");
        var response = await controller.UploadBanner(raceEvent.EventId, pdfFile);

        Assert.Equal(StatusCodes.Status400BadRequest, TestHarness.StatusCodeOf(response));
        Assert.Empty(blobStorage.UploadedFileNames);
    }

    [Fact]
    public async Task An_image_over_five_megabytes_is_rejected()
    {
        using var db = TestHarness.NewDatabase();
        var organiser = db.AddUser(RoleNames.Organiser, "thabo.nkosi@raceday.co.za");
        var raceEvent = db.AddEvent(organiser.UserId);
        var blobStorage = new FakeBlobStorage();
        var controller = new EventsController(db, blobStorage).SignedInAs(organiser);

        var hugeFile = TestHarness.ImageFile(sizeInBytes: (int)ImageUpload.MaxSizeInBytes + 1);
        var response = await controller.UploadBanner(raceEvent.EventId, hugeFile);

        Assert.Equal(StatusCodes.Status400BadRequest, TestHarness.StatusCodeOf(response));
        Assert.Empty(blobStorage.UploadedFileNames);
    }
}
