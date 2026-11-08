using Microsoft.AspNetCore.Http;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Tests.Support;

namespace RaceDay.Api.Tests;

public class EnrolmentTests
{
    [Fact]
    public async Task Participant_can_enter_an_event_and_the_entry_links_all_three_rows()
    {
        using var db = TestHarness.NewDatabase();
        var organiser = db.AddUser(RoleNames.Organiser, "thabo.nkosi@raceday.co.za");
        var participant = db.AddUser(RoleNames.Participant, "sipho.dlamini@raceday.co.za", "Sipho Dlamini");
        var raceEvent = db.AddEvent(organiser.UserId);
        var category = db.AddCategory(raceEvent.EventId);
        var controller = new EnrolmentsController(db).SignedInAs(participant);

        var response = await controller.Enrol(
            raceEvent.EventId,
            new CreateEnrolmentRequest { CategoryId = category.CategoryId });

        Assert.Equal(StatusCodes.Status201Created, TestHarness.StatusCodeOf(response));

        var saved = db.Enrolments.Single();
        Assert.Equal(participant.UserId, saved.ParticipantId);
        Assert.Equal(raceEvent.EventId, saved.EventId);
        Assert.Equal(category.CategoryId, saved.CategoryId);
        Assert.Equal(EnrolmentStatuses.Confirmed, saved.Status);
    }

    [Fact]
    public async Task The_entry_is_always_recorded_against_the_logged_in_participant()
    {
        using var db = TestHarness.NewDatabase();
        var organiser = db.AddUser(RoleNames.Organiser, "thabo.nkosi@raceday.co.za");
        var participant = db.AddUser(RoleNames.Participant, "sipho.dlamini@raceday.co.za");
        var somebodyElse = db.AddUser(RoleNames.Participant, "lerato.mokoena@raceday.co.za");
        var raceEvent = db.AddEvent(organiser.UserId);
        var category = db.AddCategory(raceEvent.EventId);
        var controller = new EnrolmentsController(db).SignedInAs(participant);

        await controller.Enrol(raceEvent.EventId, new CreateEnrolmentRequest { CategoryId = category.CategoryId });

        Assert.Equal(participant.UserId, db.Enrolments.Single().ParticipantId);
        Assert.DoesNotContain(somebodyElse.UserId, db.Enrolments.Select(e => e.ParticipantId));
    }

    [Fact]
    public async Task Entering_the_same_event_a_second_time_is_refused()
    {
        using var db = TestHarness.NewDatabase();
        var organiser = db.AddUser(RoleNames.Organiser, "thabo.nkosi@raceday.co.za");
        var participant = db.AddUser(RoleNames.Participant, "sipho.dlamini@raceday.co.za");
        var raceEvent = db.AddEvent(organiser.UserId);
        var category = db.AddCategory(raceEvent.EventId);
        var controller = new EnrolmentsController(db).SignedInAs(participant);
        var request = new CreateEnrolmentRequest { CategoryId = category.CategoryId };

        await controller.Enrol(raceEvent.EventId, request);
        var secondAttempt = await controller.Enrol(raceEvent.EventId, request);

        Assert.Equal(StatusCodes.Status409Conflict, TestHarness.StatusCodeOf(secondAttempt));
        Assert.Single(db.Enrolments);
    }

    [Fact]
    public async Task A_category_belonging_to_a_different_event_is_refused()
    {
        using var db = TestHarness.NewDatabase();
        var organiser = db.AddUser(RoleNames.Organiser, "thabo.nkosi@raceday.co.za");
        var participant = db.AddUser(RoleNames.Participant, "sipho.dlamini@raceday.co.za");
        var wantedEvent = db.AddEvent(organiser.UserId, "Coastal 10K");
        var otherEvent = db.AddEvent(organiser.UserId, "City Cycle Challenge");
        var otherCategory = db.AddCategory(otherEvent.EventId, "21km Open");
        var controller = new EnrolmentsController(db).SignedInAs(participant);

        var response = await controller.Enrol(
            wantedEvent.EventId,
            new CreateEnrolmentRequest { CategoryId = otherCategory.CategoryId });

        Assert.Equal(StatusCodes.Status400BadRequest, TestHarness.StatusCodeOf(response));
        Assert.Empty(db.Enrolments);
    }

    [Fact]
    public async Task Entering_an_event_that_does_not_exist_gives_404()
    {
        using var db = TestHarness.NewDatabase();
        var participant = db.AddUser(RoleNames.Participant, "sipho.dlamini@raceday.co.za");
        var controller = new EnrolmentsController(db).SignedInAs(participant);

        var response = await controller.Enrol(4321, new CreateEnrolmentRequest { CategoryId = 1 });

        Assert.Equal(StatusCodes.Status404NotFound, TestHarness.StatusCodeOf(response));
    }

    [Fact]
    public async Task My_entries_leaves_out_other_participants_entries()
    {
        using var db = TestHarness.NewDatabase();
        var organiser = db.AddUser(RoleNames.Organiser, "thabo.nkosi@raceday.co.za");
        var participant = db.AddUser(RoleNames.Participant, "sipho.dlamini@raceday.co.za");
        var somebodyElse = db.AddUser(RoleNames.Participant, "lerato.mokoena@raceday.co.za");
        var raceEvent = db.AddEvent(organiser.UserId);
        var category = db.AddCategory(raceEvent.EventId);
        db.AddEnrolment(participant.UserId, raceEvent.EventId, category.CategoryId);
        db.AddEnrolment(somebodyElse.UserId, raceEvent.EventId, category.CategoryId);
        var controller = new EnrolmentsController(db).SignedInAs(participant);

        var response = await controller.GetMyEnrolments();

        var mine = TestHarness.BodyOf<List<MyEnrolmentResponse>>(response.Result);
        Assert.Equal(raceEvent.EventId, Assert.Single(mine).EventId);
        Assert.Equal(2, db.Enrolments.Count());
    }

    [Fact]
    public async Task Organiser_sees_everyone_entered_in_their_own_event()
    {
        using var db = TestHarness.NewDatabase();
        var organiser = db.AddUser(RoleNames.Organiser, "thabo.nkosi@raceday.co.za");
        var firstParticipant = db.AddUser(RoleNames.Participant, "sipho.dlamini@raceday.co.za", "Sipho Dlamini");
        var secondParticipant = db.AddUser(RoleNames.Participant, "lerato.mokoena@raceday.co.za", "Lerato Mokoena");
        var raceEvent = db.AddEvent(organiser.UserId);
        var category = db.AddCategory(raceEvent.EventId);
        db.AddEnrolment(firstParticipant.UserId, raceEvent.EventId, category.CategoryId);
        db.AddEnrolment(secondParticipant.UserId, raceEvent.EventId, category.CategoryId);
        var controller = new EnrolmentsController(db).SignedInAs(organiser);

        var response = await controller.GetEnrolmentsForEvent(raceEvent.EventId);

        var startList = TestHarness.BodyOf<List<EventEnrolmentResponse>>(response.Result);
        Assert.Equal(
            new[] { "Lerato Mokoena", "Sipho Dlamini" },
            startList.Select(entry => entry.ParticipantName));
    }

    [Fact]
    public async Task An_organiser_cannot_see_the_start_list_of_somebody_elses_event()
    {
        using var db = TestHarness.NewDatabase();
        var owner = db.AddUser(RoleNames.Organiser, "thabo.nkosi@raceday.co.za");
        var otherOrganiser = db.AddUser(RoleNames.Organiser, "amanda.vdm@raceday.co.za");
        var raceEvent = db.AddEvent(owner.UserId);
        var controller = new EnrolmentsController(db).SignedInAs(otherOrganiser);

        var response = await controller.GetEnrolmentsForEvent(raceEvent.EventId);

        Assert.Equal(StatusCodes.Status403Forbidden, TestHarness.StatusCodeOf(response));
    }

    [Fact]
    public async Task The_start_list_shows_the_result_once_one_has_been_captured()
    {
        using var db = TestHarness.NewDatabase();
        var organiser = db.AddUser(RoleNames.Organiser, "thabo.nkosi@raceday.co.za");
        var finisher = db.AddUser(RoleNames.Participant, "sipho.dlamini@raceday.co.za", "Sipho Dlamini");
        var stillWaiting = db.AddUser(RoleNames.Participant, "lerato.mokoena@raceday.co.za", "Lerato Mokoena");
        var raceEvent = db.AddEvent(organiser.UserId, "Coastal 10K", DateTime.Now.AddMonths(-1));
        var category = db.AddCategory(raceEvent.EventId);
        var finished = db.AddEnrolment(finisher.UserId, raceEvent.EventId, category.CategoryId);
        db.AddEnrolment(stillWaiting.UserId, raceEvent.EventId, category.CategoryId);
        db.AddResult(finished.EnrolmentId, "00:52:18", 47);
        var controller = new EnrolmentsController(db).SignedInAs(organiser);

        var response = await controller.GetEnrolmentsForEvent(raceEvent.EventId);

        var startList = TestHarness.BodyOf<List<EventEnrolmentResponse>>(response.Result);
        var withResult = startList.Single(entry => entry.ParticipantName == "Sipho Dlamini");
        var withoutResult = startList.Single(entry => entry.ParticipantName == "Lerato Mokoena");

        Assert.Equal("00:52:18", withResult.FinishTime);
        Assert.Equal(47, withResult.FinishingPosition);
        Assert.Null(withoutResult.FinishTime);
        Assert.Null(withoutResult.FinishingPosition);
    }
}
