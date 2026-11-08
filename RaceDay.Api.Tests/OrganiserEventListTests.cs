using Microsoft.AspNetCore.Http;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Tests.Support;

namespace RaceDay.Api.Tests;

public class OrganiserEventListTests
{
    [Fact]
    public async Task My_events_leaves_out_events_created_by_other_organisers()
    {
        using var db = TestHarness.NewDatabase();
        var organiser = db.AddUser(RoleNames.Organiser, "thabo.nkosi@raceday.co.za");
        var otherOrganiser = db.AddUser(RoleNames.Organiser, "amanda.vdm@raceday.co.za");
        db.AddEvent(organiser.UserId, "Mine");
        db.AddEvent(otherOrganiser.UserId, "Theirs");
        var controller = new EventsController(db, new FakeBlobStorage()).SignedInAs(organiser);

        var response = await controller.GetMyEvents();

        var events = TestHarness.BodyOf<List<OrganiserEventResponse>>(response.Result);
        Assert.Equal("Mine", Assert.Single(events).EventName);
    }

    [Fact]
    public async Task My_events_counts_the_entries_on_each_event()
    {
        using var db = TestHarness.NewDatabase();
        var organiser = db.AddUser(RoleNames.Organiser, "thabo.nkosi@raceday.co.za");
        var firstParticipant = db.AddUser(RoleNames.Participant, "sipho.dlamini@raceday.co.za");
        var secondParticipant = db.AddUser(RoleNames.Participant, "lerato.mokoena@raceday.co.za");
        var busyEvent = db.AddEvent(organiser.UserId, "Busy race", DateTime.Now.AddMonths(1));
        var quietEvent = db.AddEvent(organiser.UserId, "Quiet race", DateTime.Now.AddMonths(2));
        var category = db.AddCategory(busyEvent.EventId);
        db.AddEnrolment(firstParticipant.UserId, busyEvent.EventId, category.CategoryId);
        db.AddEnrolment(secondParticipant.UserId, busyEvent.EventId, category.CategoryId);
        var controller = new EventsController(db, new FakeBlobStorage()).SignedInAs(organiser);

        var response = await controller.GetMyEvents();

        var events = TestHarness.BodyOf<List<OrganiserEventResponse>>(response.Result);
        Assert.Equal(2, events.Single(e => e.EventId == busyEvent.EventId).EnrolmentCount);
        Assert.Equal(0, events.Single(e => e.EventId == quietEvent.EventId).EnrolmentCount);
    }

    [Fact]
    public async Task The_upcoming_filter_drops_events_that_have_already_happened()
    {
        using var db = TestHarness.NewDatabase();
        var organiser = db.AddUser(RoleNames.Organiser, "thabo.nkosi@raceday.co.za");
        db.AddEvent(organiser.UserId, "Already run", DateTime.Now.AddMonths(-1));
        db.AddEvent(organiser.UserId, "Still to come", DateTime.Now.AddMonths(1));
        var controller = new EventsController(db, new FakeBlobStorage()).SignedInAs(organiser);

        var response = await controller.GetEvents(null, null, null, true);

        var events = TestHarness.BodyOf<List<EventSummaryResponse>>(response.Result);
        Assert.Equal("Still to come", Assert.Single(events).EventName);
    }

    [Fact]
    public async Task Leaving_the_upcoming_filter_off_returns_past_events_too()
    {
        using var db = TestHarness.NewDatabase();
        var organiser = db.AddUser(RoleNames.Organiser, "thabo.nkosi@raceday.co.za");
        db.AddEvent(organiser.UserId, "Already run", DateTime.Now.AddMonths(-1));
        db.AddEvent(organiser.UserId, "Still to come", DateTime.Now.AddMonths(1));
        var controller = new EventsController(db, new FakeBlobStorage()).SignedInAs(organiser);

        var response = await controller.GetEvents(null, null, null, null);

        var events = TestHarness.BodyOf<List<EventSummaryResponse>>(response.Result);
        Assert.Equal(2, events.Count);
    }
}
