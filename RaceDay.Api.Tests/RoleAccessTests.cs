using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;
using RaceDay.Api.Tests.Support;

namespace RaceDay.Api.Tests;

/// <summary>
/// Tests for the SessionAuthorize filter itself, plus a check that each
/// endpoint is actually decorated with the role the Part 1 plan assigns to it.
/// Running the filter directly is the only way to see the 401 and 403 answers,
/// because a unit test calling an action method skips the filter pipeline.
/// </summary>
public class RoleAccessTests
{
    private static AuthorizationFilterContext ContextFor(int? userId, string? roleName)
    {
        var httpContext = new DefaultHttpContext { Session = new FakeSession() };
        if (userId is not null && roleName is not null)
        {
            httpContext.Session.StartUserSession(userId.Value, roleName);
        }

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    private static int? StatusCodeFrom(AuthorizationFilterContext context)
    {
        return context.Result is null ? null : TestHarness.StatusCodeOf(context.Result);
    }

    [Fact]
    public void A_request_with_no_session_is_answered_with_401()
    {
        var context = ContextFor(null, null);

        new SessionAuthorizeAttribute().OnAuthorization(context);

        Assert.Equal(StatusCodes.Status401Unauthorized, StatusCodeFrom(context));
    }

    [Theory]
    [InlineData(RoleNames.Organiser)]
    [InlineData(RoleNames.Participant)]
    public void Either_role_gets_through_an_endpoint_that_only_needs_a_session(string roleName)
    {
        var context = ContextFor(7, roleName);

        new SessionAuthorizeAttribute().OnAuthorization(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void A_participant_is_turned_away_from_an_organiser_endpoint()
    {
        var context = ContextFor(7, RoleNames.Participant);

        new SessionAuthorizeAttribute(RoleNames.Organiser).OnAuthorization(context);

        Assert.Equal(StatusCodes.Status403Forbidden, StatusCodeFrom(context));
        var body = TestHarness.BodyOf<MessageResponse>(context.Result);
        Assert.Contains(RoleNames.Organiser, body.Message);
    }

    [Fact]
    public void An_organiser_is_turned_away_from_a_participant_endpoint()
    {
        var context = ContextFor(7, RoleNames.Organiser);

        new SessionAuthorizeAttribute(RoleNames.Participant).OnAuthorization(context);

        Assert.Equal(StatusCodes.Status403Forbidden, StatusCodeFrom(context));
    }

    [Fact]
    public void The_right_role_gets_through()
    {
        var context = ContextFor(7, RoleNames.Organiser);

        new SessionAuthorizeAttribute(RoleNames.Organiser).OnAuthorization(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void No_session_on_a_role_restricted_endpoint_is_401_rather_than_403()
    {
        var context = ContextFor(null, null);

        new SessionAuthorizeAttribute(RoleNames.Organiser).OnAuthorization(context);

        Assert.Equal(StatusCodes.Status401Unauthorized, StatusCodeFrom(context));
    }

    [Theory]
    [InlineData(typeof(ProfileController))]
    [InlineData(typeof(EventsController))]
    [InlineData(typeof(CategoriesController))]
    [InlineData(typeof(EnrolmentsController))]
    [InlineData(typeof(ResultsController))]
    public void Every_non_public_controller_requires_a_session(Type controllerType)
    {
        Assert.NotNull(controllerType.GetCustomAttribute<SessionAuthorizeAttribute>());
    }

    [Fact]
    public void The_auth_controller_stays_open_so_people_can_register_and_log_in()
    {
        Assert.Null(typeof(AuthController).GetCustomAttribute<SessionAuthorizeAttribute>());
        Assert.NotNull(
            typeof(AuthController).GetMethod(nameof(AuthController.Logout))!
                .GetCustomAttribute<SessionAuthorizeAttribute>());
    }

    [Theory]
    [InlineData(typeof(EventsController), nameof(EventsController.CreateEvent), RoleNames.Organiser)]
    [InlineData(typeof(EventsController), nameof(EventsController.UpdateEvent), RoleNames.Organiser)]
    [InlineData(typeof(EventsController), nameof(EventsController.DeleteEvent), RoleNames.Organiser)]
    [InlineData(typeof(CategoriesController), nameof(CategoriesController.CreateCategory), RoleNames.Organiser)]
    [InlineData(typeof(CategoriesController), nameof(CategoriesController.UpdateCategory), RoleNames.Organiser)]
    [InlineData(typeof(CategoriesController), nameof(CategoriesController.DeleteCategory), RoleNames.Organiser)]
    [InlineData(typeof(EnrolmentsController), nameof(EnrolmentsController.GetEnrolmentsForEvent), RoleNames.Organiser)]
    [InlineData(typeof(ResultsController), nameof(ResultsController.CaptureResult), RoleNames.Organiser)]
    [InlineData(typeof(EnrolmentsController), nameof(EnrolmentsController.Enrol), RoleNames.Participant)]
    [InlineData(typeof(EnrolmentsController), nameof(EnrolmentsController.GetMyEnrolments), RoleNames.Participant)]
    [InlineData(typeof(ResultsController), nameof(ResultsController.GetMyResults), RoleNames.Participant)]
    public void Each_role_restricted_endpoint_asks_for_the_role_the_plan_gives_it(
        Type controllerType, string actionName, string expectedRole)
    {
        var action = controllerType.GetMethod(actionName);
        Assert.NotNull(action);

        var attribute = action.GetCustomAttribute<SessionAuthorizeAttribute>();
        Assert.NotNull(attribute);
        Assert.Equal(new[] { expectedRole }, attribute.AllowedRoles);
    }

    [Theory]
    [InlineData(typeof(EventsController), nameof(EventsController.GetEvents))]
    [InlineData(typeof(EventsController), nameof(EventsController.GetEvent))]
    [InlineData(typeof(CategoriesController), nameof(CategoriesController.GetCategories))]
    [InlineData(typeof(ProfileController), nameof(ProfileController.GetMyProfile))]
    [InlineData(typeof(ProfileController), nameof(ProfileController.UpdateMyProfile))]
    public void Endpoints_open_to_both_roles_add_no_role_restriction_of_their_own(
        Type controllerType, string actionName)
    {
        var action = controllerType.GetMethod(actionName);
        Assert.NotNull(action);
        Assert.Null(action.GetCustomAttribute<SessionAuthorizeAttribute>());
    }
}
