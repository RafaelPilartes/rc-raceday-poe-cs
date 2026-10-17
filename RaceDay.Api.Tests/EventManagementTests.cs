using Microsoft.AspNetCore.Http;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Tests.Support;

namespace RaceDay.Api.Tests;

public class EventManagementTests
{
    private static SaveEventRequest ValidEvent(string eventType = EventTypes.Run)
    {
        return new SaveEventRequest
        {
            EventName = "Cape Town Coastal 10K",
            Description = "A flat, scenic 10km run along the Sea Point promenade.",
            EventDate = new DateTime(2026, 6, 15, 7, 0, 0),
            Location = "Sea Point Promenade, Cape Town",
            DistanceKm = 10m,
            EventType = eventType
        };
    }

    [Fact]
    public async Task Organiser_can_create_an_event_and_it_is_recorded_against_them()
    {
        using var db = TestHarness.NewDatabase();
        var organiser = db.AddUser(RoleNames.Organiser, "thabo.nkosi@raceday.co.za", "Thabo Nkosi");
        var controller = new EventsController(db).SignedInAs(organiser);

        var response = await controller.CreateEvent(ValidEvent());

        Assert.Equal(StatusCodes.Status201Created, TestHarness.StatusCodeOf(response));

        var saved = db.Events.Single();
        Assert.Equal(organiser.UserId, saved.OrganiserId);
        Assert.Equal("Cape Town Coastal 10K", saved.EventName);
        Assert.Equal(EventTypes.Run, saved.EventType);
        Assert.Equal(10m, saved.DistanceKm);
    }

    [Fact]
    public async Task An_event_type_outside_run_walk_cycle_is_rejected()
    {
        using var db = TestHarness.NewDatabase();
        var organiser = db.AddUser(RoleNames.Organiser, "thabo.nkosi@raceday.co.za");
        var controller = new EventsController(db).SignedInAs(organiser);

        var response = await controller.CreateEvent(ValidEvent("Swim"));

        Assert.Equal(StatusCodes.Status400BadRequest, TestHarness.StatusCodeOf(response));
        Assert.Empty(db.Events);
    }

    [Fact]
    public async Task Organiser_can_update_an_event_they_own()
    {
        using var db = TestHarness.NewDatabase();
        var organiser = db.AddUser(RoleNames.Organiser, "thabo.nkosi@raceday.co.za");
        var raceEvent = db.AddEvent(organiser.UserId);
        var controller = new EventsController(db).SignedInAs(organiser);

        var changes = ValidEvent(EventTypes.Walk);
        changes.EventName = "Cape Town Coastal 10K Walk";
        changes.Location = "Green Point, Cape Town";

        var response = await controller.UpdateEvent(raceEvent.EventId, changes);

        Assert.Equal(StatusCodes.Status200OK, TestHarness.StatusCodeOf(response));

        var saved = db.Events.Single();
        Assert.Equal("Cape Town Coastal 10K Walk", saved.EventName);
        Assert.Equal("Green Point, Cape Town", saved.Location);
        Assert.Equal(EventTypes.Walk, saved.EventType);
    }

    [Fact]
    public async Task One_organiser_cannot_update_another_organisers_event()
    {
        using var db = TestHarness.NewDatabase();
        var owner = db.AddUser(RoleNames.Organiser, "thabo.nkosi@raceday.co.za", "Thabo Nkosi");
        var otherOrganiser = db.AddUser(RoleNames.Organiser, "amanda.vdm@raceday.co.za", "Amanda van der Merwe");
        var raceEvent = db.AddEvent(owner.UserId);
        var controller = new EventsController(db).SignedInAs(otherOrganiser);

        var response = await controller.UpdateEvent(raceEvent.EventId, ValidEvent());

        Assert.Equal(StatusCodes.Status403Forbidden, TestHarness.StatusCodeOf(response));
        Assert.Equal("Cape Town Coastal 10K", db.Events.Single().EventName);
    }

    [Fact]
    public async Task Updating_an_event_that_does_not_exist_gives_404()
    {
        using var db = TestHarness.NewDatabase();
        var organiser = db.AddUser(RoleNames.Organiser, "thabo.nkosi@raceday.co.za");
        var controller = new EventsController(db).SignedInAs(organiser);

        var response = await controller.UpdateEvent(4321, ValidEvent());

        Assert.Equal(StatusCodes.Status404NotFound, TestHarness.StatusCodeOf(response));
    }

    [Fact]
    public async Task Deleting_an_event_takes_its_categories_with_it()
    {
        using var db = TestHarness.NewDatabase();
        var organiser = db.AddUser(RoleNames.Organiser, "thabo.nkosi@raceday.co.za");
        var raceEvent = db.AddEvent(organiser.UserId);
        db.AddCategory(raceEvent.EventId);
        db.AddCategory(raceEvent.EventId, "Veteran 40+");
        var controller = new EventsController(db).SignedInAs(organiser);

        var result = await controller.DeleteEvent(raceEvent.EventId);

        Assert.Equal(StatusCodes.Status204NoContent, TestHarness.StatusCodeOf(result));
        Assert.Empty(db.Events);
        Assert.Empty(db.Categories);
    }

    [Fact]
    public async Task An_event_with_entries_already_in_it_is_not_deleted()
    {
        using var db = TestHarness.NewDatabase();
        var organiser = db.AddUser(RoleNames.Organiser, "thabo.nkosi@raceday.co.za");
        var participant = db.AddUser(RoleNames.Participant, "sipho.dlamini@raceday.co.za");
        var raceEvent = db.AddEvent(organiser.UserId);
        var category = db.AddCategory(raceEvent.EventId);
        db.AddEnrolment(participant.UserId, raceEvent.EventId, category.CategoryId);
        var controller = new EventsController(db).SignedInAs(organiser);

        var result = await controller.DeleteEvent(raceEvent.EventId);

        Assert.Equal(StatusCodes.Status409Conflict, TestHarness.StatusCodeOf(result));
        Assert.Single(db.Events);
    }

    [Fact]
    public async Task Both_roles_see_the_same_event_list_in_date_order()
    {
        using var db = TestHarness.NewDatabase();
        var organiser = db.AddUser(RoleNames.Organiser, "thabo.nkosi@raceday.co.za");
        var participant = db.AddUser(RoleNames.Participant, "sipho.dlamini@raceday.co.za");
        db.AddEvent(organiser.UserId, "Later race", new DateTime(2026, 11, 8, 6, 30, 0));
        db.AddEvent(organiser.UserId, "Earlier race", new DateTime(2026, 6, 15, 7, 0, 0));

        var asParticipant = await new EventsController(db).SignedInAs(participant).GetEvents(null, null, null);
        var asOrganiser = await new EventsController(db).SignedInAs(organiser).GetEvents(null, null, null);

        var participantView = TestHarness.BodyOf<List<EventSummaryResponse>>(asParticipant.Result);
        var organiserView = TestHarness.BodyOf<List<EventSummaryResponse>>(asOrganiser.Result);

        Assert.Equal(new[] { "Earlier race", "Later race" }, participantView.Select(e => e.EventName));
        Assert.Equal(participantView.Select(e => e.EventId), organiserView.Select(e => e.EventId));
    }

    [Fact]
    public async Task The_event_type_filter_only_returns_that_type()
    {
        using var db = TestHarness.NewDatabase();
        var organiser = db.AddUser(RoleNames.Organiser, "thabo.nkosi@raceday.co.za");
        db.AddEvent(organiser.UserId, "Coastal 10K");
        var cycleEvent = db.AddEvent(organiser.UserId, "City Cycle Challenge");
        cycleEvent.EventType = EventTypes.Cycle;
        db.SaveChanges();
        var controller = new EventsController(db).SignedInAs(organiser);

        var response = await controller.GetEvents(null, null, "cycle");

        var events = TestHarness.BodyOf<List<EventSummaryResponse>>(response.Result);
        Assert.Equal("City Cycle Challenge", Assert.Single(events).EventName);
    }

    [Fact]
    public async Task Asking_for_an_event_that_does_not_exist_gives_404()
    {
        using var db = TestHarness.NewDatabase();
        var participant = db.AddUser(RoleNames.Participant, "sipho.dlamini@raceday.co.za");
        var controller = new EventsController(db).SignedInAs(participant);

        var response = await controller.GetEvent(4321);

        Assert.Equal(StatusCodes.Status404NotFound, TestHarness.StatusCodeOf(response));
    }
}
