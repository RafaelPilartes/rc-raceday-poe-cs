using Microsoft.AspNetCore.Http;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Tests.Support;

namespace RaceDay.Api.Tests;

public class CategoryTests
{
    private static SaveCategoryRequest OpenCategory(string name = "Open 20-39")
    {
        return new SaveCategoryRequest
        {
            CategoryName = name,
            MinAge = 20,
            MaxAge = 39,
            DistanceKm = 10m
        };
    }

    [Fact]
    public async Task Organiser_can_add_a_category_to_an_event_they_own()
    {
        using var db = TestHarness.NewDatabase();
        var organiser = db.AddUser(RoleNames.Organiser, "thabo.nkosi@raceday.co.za");
        var raceEvent = db.AddEvent(organiser.UserId);
        var controller = new CategoriesController(db).SignedInAs(organiser);

        var response = await controller.CreateCategory(raceEvent.EventId, OpenCategory());

        Assert.Equal(StatusCodes.Status201Created, TestHarness.StatusCodeOf(response));

        var saved = db.Categories.Single();
        Assert.Equal(raceEvent.EventId, saved.EventId);
        Assert.Equal("Open 20-39", saved.CategoryName);
        Assert.Equal(20, saved.MinAge);
        Assert.Equal(39, saved.MaxAge);
    }

    [Fact]
    public async Task An_organiser_cannot_add_a_category_to_somebody_elses_event()
    {
        using var db = TestHarness.NewDatabase();
        var owner = db.AddUser(RoleNames.Organiser, "thabo.nkosi@raceday.co.za");
        var otherOrganiser = db.AddUser(RoleNames.Organiser, "amanda.vdm@raceday.co.za");
        var raceEvent = db.AddEvent(owner.UserId);
        var controller = new CategoriesController(db).SignedInAs(otherOrganiser);

        var response = await controller.CreateCategory(raceEvent.EventId, OpenCategory());

        Assert.Equal(StatusCodes.Status403Forbidden, TestHarness.StatusCodeOf(response));
        Assert.Empty(db.Categories);
    }

    [Fact]
    public async Task An_age_range_that_runs_backwards_is_rejected()
    {
        using var db = TestHarness.NewDatabase();
        var organiser = db.AddUser(RoleNames.Organiser, "thabo.nkosi@raceday.co.za");
        var raceEvent = db.AddEvent(organiser.UserId);
        var controller = new CategoriesController(db).SignedInAs(organiser);

        var request = OpenCategory("Broken range");
        request.MinAge = 50;
        request.MaxAge = 20;

        var response = await controller.CreateCategory(raceEvent.EventId, request);

        Assert.Equal(StatusCodes.Status400BadRequest, TestHarness.StatusCodeOf(response));
        Assert.Empty(db.Categories);
    }

    [Fact]
    public async Task A_category_with_no_age_limits_is_allowed()
    {
        using var db = TestHarness.NewDatabase();
        var organiser = db.AddUser(RoleNames.Organiser, "thabo.nkosi@raceday.co.za");
        var raceEvent = db.AddEvent(organiser.UserId);
        var controller = new CategoriesController(db).SignedInAs(organiser);

        var request = OpenCategory("21km Open");
        request.MinAge = null;
        request.MaxAge = null;

        var response = await controller.CreateCategory(raceEvent.EventId, request);

        Assert.Equal(StatusCodes.Status201Created, TestHarness.StatusCodeOf(response));
        Assert.Null(db.Categories.Single().MinAge);
    }

    [Theory]
    [InlineData(RoleNames.Organiser)]
    [InlineData(RoleNames.Participant)]
    public async Task Either_role_can_list_the_categories_of_an_event(string roleName)
    {
        using var db = TestHarness.NewDatabase();
        var organiser = db.AddUser(RoleNames.Organiser, "thabo.nkosi@raceday.co.za");
        var caller = roleName == RoleNames.Organiser
            ? organiser
            : db.AddUser(RoleNames.Participant, "sipho.dlamini@raceday.co.za");
        var raceEvent = db.AddEvent(organiser.UserId);
        db.AddCategory(raceEvent.EventId, "Veteran 40+");
        db.AddCategory(raceEvent.EventId, "Open 20-39");
        var controller = new CategoriesController(db).SignedInAs(caller);

        var response = await controller.GetCategories(raceEvent.EventId);

        var categories = TestHarness.BodyOf<List<CategoryResponse>>(response.Result);
        Assert.Equal(
            new[] { "Open 20-39", "Veteran 40+" },
            categories.Select(category => category.CategoryName));
    }

    [Fact]
    public async Task Listing_categories_for_an_event_that_does_not_exist_gives_404()
    {
        using var db = TestHarness.NewDatabase();
        var participant = db.AddUser(RoleNames.Participant, "sipho.dlamini@raceday.co.za");
        var controller = new CategoriesController(db).SignedInAs(participant);

        var response = await controller.GetCategories(4321);

        Assert.Equal(StatusCodes.Status404NotFound, TestHarness.StatusCodeOf(response));
    }

    [Fact]
    public async Task Organiser_can_rename_a_category_on_their_own_event()
    {
        using var db = TestHarness.NewDatabase();
        var organiser = db.AddUser(RoleNames.Organiser, "thabo.nkosi@raceday.co.za");
        var raceEvent = db.AddEvent(organiser.UserId);
        var category = db.AddCategory(raceEvent.EventId);
        var controller = new CategoriesController(db).SignedInAs(organiser);

        var response = await controller.UpdateCategory(category.CategoryId, OpenCategory("Open 20-39 Renamed"));

        Assert.Equal(StatusCodes.Status200OK, TestHarness.StatusCodeOf(response));
        Assert.Equal("Open 20-39 Renamed", db.Categories.Single().CategoryName);
    }

    [Fact]
    public async Task A_category_somebody_has_already_entered_is_not_deleted()
    {
        using var db = TestHarness.NewDatabase();
        var organiser = db.AddUser(RoleNames.Organiser, "thabo.nkosi@raceday.co.za");
        var participant = db.AddUser(RoleNames.Participant, "sipho.dlamini@raceday.co.za");
        var raceEvent = db.AddEvent(organiser.UserId);
        var category = db.AddCategory(raceEvent.EventId);
        db.AddEnrolment(participant.UserId, raceEvent.EventId, category.CategoryId);
        var controller = new CategoriesController(db).SignedInAs(organiser);

        var result = await controller.DeleteCategory(category.CategoryId);

        Assert.Equal(StatusCodes.Status409Conflict, TestHarness.StatusCodeOf(result));
        Assert.Single(db.Categories);
    }

    [Fact]
    public async Task An_unused_category_can_be_deleted()
    {
        using var db = TestHarness.NewDatabase();
        var organiser = db.AddUser(RoleNames.Organiser, "thabo.nkosi@raceday.co.za");
        var raceEvent = db.AddEvent(organiser.UserId);
        var category = db.AddCategory(raceEvent.EventId);
        var controller = new CategoriesController(db).SignedInAs(organiser);

        var result = await controller.DeleteCategory(category.CategoryId);

        Assert.Equal(StatusCodes.Status204NoContent, TestHarness.StatusCodeOf(result));
        Assert.Empty(db.Categories);
    }
}
