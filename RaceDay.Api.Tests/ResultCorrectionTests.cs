using Microsoft.AspNetCore.Http;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Tests.Support;

namespace RaceDay.Api.Tests;

public class ResultCorrectionTests
{
    private static CaptureResultRequest FinishedIn(string finishTime, int position)
    {
        return new CaptureResultRequest { FinishTime = finishTime, FinishingPosition = position };
    }

    [Fact]
    public async Task Organiser_can_correct_a_result_on_their_own_event()
    {
        using var db = TestHarness.NewDatabase();
        var organiser = db.AddUser(RoleNames.Organiser, "thabo.nkosi@raceday.co.za");
        var participant = db.AddUser(RoleNames.Participant, "sipho.dlamini@raceday.co.za");
        var raceEvent = db.AddEvent(organiser.UserId, "Coastal 10K", DateTime.Now.AddMonths(-2));
        var category = db.AddCategory(raceEvent.EventId);
        var enrolment = db.AddEnrolment(participant.UserId, raceEvent.EventId, category.CategoryId);
        db.AddResult(enrolment.EnrolmentId, "00:52:18", 47);
        var controller = new ResultsController(db).SignedInAs(organiser);

        var response = await controller.UpdateResult(enrolment.EnrolmentId, FinishedIn("00:49:02", 31));

        Assert.Equal(StatusCodes.Status200OK, TestHarness.StatusCodeOf(response));

        var saved = db.Results.Single();
        Assert.Equal(new TimeSpan(0, 49, 2), saved.FinishTime);
        Assert.Equal(31, saved.FinishingPosition);
    }

    [Fact]
    public async Task An_organiser_cannot_correct_a_result_on_somebody_elses_event()
    {
        using var db = TestHarness.NewDatabase();
        var owner = db.AddUser(RoleNames.Organiser, "thabo.nkosi@raceday.co.za");
        var otherOrganiser = db.AddUser(RoleNames.Organiser, "amanda.vdm@raceday.co.za");
        var participant = db.AddUser(RoleNames.Participant, "sipho.dlamini@raceday.co.za");
        var raceEvent = db.AddEvent(owner.UserId, "Coastal 10K", DateTime.Now.AddMonths(-2));
        var category = db.AddCategory(raceEvent.EventId);
        var enrolment = db.AddEnrolment(participant.UserId, raceEvent.EventId, category.CategoryId);
        db.AddResult(enrolment.EnrolmentId, "00:52:18", 47);
        var controller = new ResultsController(db).SignedInAs(otherOrganiser);

        var response = await controller.UpdateResult(enrolment.EnrolmentId, FinishedIn("00:49:02", 31));

        Assert.Equal(StatusCodes.Status403Forbidden, TestHarness.StatusCodeOf(response));
        Assert.Equal(new TimeSpan(0, 52, 18), db.Results.Single().FinishTime);
    }

    [Fact]
    public async Task Correcting_an_entry_that_has_no_result_yet_gives_404()
    {
        using var db = TestHarness.NewDatabase();
        var organiser = db.AddUser(RoleNames.Organiser, "thabo.nkosi@raceday.co.za");
        var participant = db.AddUser(RoleNames.Participant, "sipho.dlamini@raceday.co.za");
        var raceEvent = db.AddEvent(organiser.UserId, "Coastal 10K", DateTime.Now.AddMonths(-2));
        var category = db.AddCategory(raceEvent.EventId);
        var enrolment = db.AddEnrolment(participant.UserId, raceEvent.EventId, category.CategoryId);
        var controller = new ResultsController(db).SignedInAs(organiser);

        var response = await controller.UpdateResult(enrolment.EnrolmentId, FinishedIn("00:49:02", 31));

        Assert.Equal(StatusCodes.Status404NotFound, TestHarness.StatusCodeOf(response));
    }

    [Fact]
    public async Task My_results_report_how_many_people_finished_the_event()
    {
        using var db = TestHarness.NewDatabase();
        var organiser = db.AddUser(RoleNames.Organiser, "thabo.nkosi@raceday.co.za");
        var participant = db.AddUser(RoleNames.Participant, "sipho.dlamini@raceday.co.za");
        var secondFinisher = db.AddUser(RoleNames.Participant, "lerato.mokoena@raceday.co.za");
        var thirdFinisher = db.AddUser(RoleNames.Participant, "amanda.vdm@raceday.co.za");
        var raceEvent = db.AddEvent(organiser.UserId, "Coastal 10K", DateTime.Now.AddMonths(-2));
        var category = db.AddCategory(raceEvent.EventId);
        var mine = db.AddEnrolment(participant.UserId, raceEvent.EventId, category.CategoryId);
        var second = db.AddEnrolment(secondFinisher.UserId, raceEvent.EventId, category.CategoryId);
        var third = db.AddEnrolment(thirdFinisher.UserId, raceEvent.EventId, category.CategoryId);
        db.AddResult(mine.EnrolmentId, "00:52:18", 2);
        db.AddResult(second.EnrolmentId, "00:48:00", 1);
        db.AddResult(third.EnrolmentId, "01:02:00", 3);
        var controller = new ResultsController(db).SignedInAs(participant);

        var response = await controller.GetMyResults();

        var results = TestHarness.BodyOf<List<MyResultResponse>>(response.Result);
        var onlyResult = Assert.Single(results);
        Assert.Equal(2, onlyResult.FinishingPosition);
        Assert.Equal(3, onlyResult.TotalFinishers);
    }
}
