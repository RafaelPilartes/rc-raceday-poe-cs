namespace RaceDay.Api.Security;

public static class SessionKeys
{
    public const string UserId = "RaceDay.UserId";
    public const string RoleName = "RaceDay.RoleName";
}

/// <summary>
/// Small wrappers so controllers read the logged-in user straight off the
/// session without repeating the key strings everywhere.
/// </summary>
public static class RaceDaySessionExtensions
{
    public static void StartUserSession(this ISession session, int userId, string roleName)
    {
        session.SetInt32(SessionKeys.UserId, userId);
        session.SetString(SessionKeys.RoleName, roleName);
    }

    public static int? GetUserId(this ISession session)
    {
        return session.GetInt32(SessionKeys.UserId);
    }

    public static string? GetRoleName(this ISession session)
    {
        return session.GetString(SessionKeys.RoleName);
    }
}
