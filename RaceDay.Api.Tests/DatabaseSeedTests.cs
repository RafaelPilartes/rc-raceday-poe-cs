using RaceDay.Api.Models;
using RaceDay.Api.Tests.Support;

namespace RaceDay.Api.Tests;

public class DatabaseSeedTests
{
    [Fact]
    public void New_database_carries_the_two_seeded_roles()
    {
        using var db = TestHarness.NewDatabase();

        var roleNames = db.Roles.Select(role => role.RoleName).OrderBy(name => name).ToList();

        Assert.Equal(new[] { RoleNames.Organiser, RoleNames.Participant }, roleNames);
    }

    [Fact]
    public void Added_user_is_linked_to_the_role_it_was_created_with()
    {
        using var db = TestHarness.NewDatabase();

        var participant = db.AddUser(RoleNames.Participant, "sipho.dlamini@raceday.co.za");

        Assert.Equal(RoleNames.Participant, participant.Role.RoleName);
        Assert.NotEqual(0, participant.UserId);
    }
}
