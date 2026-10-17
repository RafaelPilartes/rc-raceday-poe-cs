using Microsoft.AspNetCore.Http;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Tests.Support;

namespace RaceDay.Api.Tests;

public class ResultTests
{
    private static CaptureResultRequest FinishedIn(string finishTime = "00:52:18", int position = 47)
    {
        return new CaptureResultRequest { FinishTime = finishTime, FinishingPosition = position };
    }

    [Fact]
    public async Task Organiser_can_capture_a_result_once_the_race_has_been_run()
    {
        using var db = TestHarness.NewDatabase();
        var organiser = db.AddUser(RoleNames.Organiser, "thabo.nkosi@raceday.co.za");
        var participant = db.AddUser(RoleNames.Participant, "sipho.dlamini@raceday.co.za");
        var raceEvent = db.AddEvent(organiser.UserId, "Coastal 10K", DateTime.Now.AddMonths(-2));
        var category = db.AddCategory(raceEvent.EventId);
        var enrolment = db.AddEnrolment(participant.UserId, raceEvent.EventId, category.CategoryId);
        var controller = new ResultsController(db).SignedInAs(organiser);

        var response = await controller.CaptureResult(enrolment.EnrolmentId, FinishedIn());

        Assert.Equal(StatusCodes.Status201Created, TestHarness.StatusCodeOf(response));

        var saved = db.Results.Single();
        Assert.Equal(enrolment.EnrolmentId, saved.EnrolmentId);
        Assert.Equal(new TimeSpan(0, 52, 18), saved.FinishTime);
        Assert.Equal(47, saved.FinishingPosition);
        Assert.NotNull(saved.CapturedAt);
    }

    [Fact]
    public async Task A_result_cannot_be_captured_before_the_race_has_happened()
    {
        using var db = TestHarness.NewDatabase();
        var organiser = db.AddUser(RoleNames.Organiser, "thabo.nkosi@raceday.co.za");
        var participant = db.AddUser(RoleNames.Participant, "sipho.dlamini@raceday.co.za");
        var raceEvent = db.AddEvent(organiser.UserId, "Coastal 10K", DateTime.Now.AddMonths(2));
        var category = db.AddCategory(raceEvent.EventId);
        var enrolment = db.AddEnrolment(participant.UserId, raceEvent.EventId, category.CategoryId);
        var controller = new ResultsController(db).SignedInAs(organiser);

        var response = await controller.CaptureResult(enrolment.EnrolmentId, FinishedIn());

        Assert.Equal(StatusCodes.Status400BadRequest, TestHarness.StatusCodeOf(response));
        Assert.Empty(db.Results);
    }

    [Fact]
    public async Task An_entry_can_only_hold_one_result()
    {
        using var db = TestHarness.NewDatabase();
        var organiser = db.AddUser(RoleNames.Organiser, "thabo.nkosi@raceday.co.za");
        var participant = db.AddUser(RoleNames.Participant, "sipho.dlamini@raceday.co.za");
        var raceEvent = db.AddEvent(organiser.UserId, "Coastal 10K", DateTime.Now.AddMonths(-2));
        var category = db.AddCategory(raceEvent.EventId);
        var enrolment = db.AddEnrolment(participant.UserId, raceEvent.EventId, category.CategoryId);
        var controller = new ResultsController(db).SignedInAs(organiser);

        await controller.CaptureResult(enrolment.EnrolmentId, FinishedIn());
        var secondAttempt = await controller.CaptureResult(enrolment.EnrolmentId, FinishedIn("00:49:02", 31));

        Assert.Equal(StatusCodes.Status409Conflict, TestHarness.StatusCodeOf(secondAttempt));
        Assert.Equal(new TimeSpan(0, 52, 18), db.Results.Single().FinishTime);
    }

    [Fact]
    public async Task An_organiser_cannot_capture_results_on_an_event_they_do_not_own()
    {
        using var db = TestHarness.NewDatabase();
        var owner = db.AddUser(RoleNames.Organiser, "thabo.nkosi@raceday.co.za");
        var otherOrganiser = db.AddUser(RoleNames.Organiser, "amanda.vdm@raceday.co.za");
        var participant = db.AddUser(RoleNames.Participant, "sipho.dlamini@raceday.co.za");
        var raceEvent = db.AddEvent(owner.UserId, "Coastal 10K", DateTime.Now.AddMonths(-2));
        var category = db.AddCategory(raceEvent.EventId);
        var enrolment = db.AddEnrolment(participant.UserId, raceEvent.EventId, category.CategoryId);
        var controller = new ResultsController(db).SignedInAs(otherOrganiser);

        var response = await controller.CaptureResult(enrolment.EnrolmentId, FinishedIn());

        Assert.Equal(StatusCodes.Status403Forbidden, TestHarness.StatusCodeOf(response));
        Assert.Empty(db.Results);
    }

    [Fact]
    public async Task Capturing_against_an_entry_that_does_not_exist_gives_404()
    {
        using var db = TestHarness.NewDatabase();
        var organiser = db.AddUser(RoleNames.Organiser, "thabo.nkosi@raceday.co.za");
        var controller = new ResultsController(db).SignedInAs(organiser);

        var response = await controller.CaptureResult(4321, FinishedIn());

        Assert.Equal(StatusCodes.Status404NotFound, TestHarness.StatusCodeOf(response));
    }

    [Fact]
    public async Task A_participant_only_sees_their_own_results()
    {
        using var db = TestHarness.NewDatabase();
        var organiser = db.AddUser(RoleNames.Organiser, "thabo.nkosi@raceday.co.za");
        var participant = db.AddUser(RoleNames.Participant, "sipho.dlamini@raceday.co.za");
        var somebodyElse = db.AddUser(RoleNames.Participant, "lerato.mokoena@raceday.co.za");
        var raceEvent = db.AddEvent(organiser.UserId, "Coastal 10K", DateTime.Now.AddMonths(-2));
        var category = db.AddCategory(raceEvent.EventId);
        var mine = db.AddEnrolment(participant.UserId, raceEvent.EventId, category.CategoryId);
        var theirs = db.AddEnrolment(somebodyElse.UserId, raceEvent.EventId, category.CategoryId);
        db.AddResult(mine.EnrolmentId, "00:52:18", 47);
        db.AddResult(theirs.EnrolmentId, "00:58:03", 112);
        var controller = new ResultsController(db).SignedInAs(participant);

        var response = await controller.GetMyResults();

        var results = TestHarness.BodyOf<List<MyResultResponse>>(response.Result);
        var onlyResult = Assert.Single(results);
        Assert.Equal("00:52:18", onlyResult.FinishTime);
        Assert.Equal(47, onlyResult.FinishingPosition);
        Assert.Equal(2, db.Results.Count());
    }
}
