namespace RaceDay.Web.Security;

public static class SessionKeys
{
    public const string ApiCookie = "RaceDay.ApiCookie";
    public const string UserId = "RaceDay.UserId";
    public const string RoleName = "RaceDay.RoleName";
    public const string FullName = "RaceDay.FullName";
}

public static class RaceDaySessionExtensions
{
    public static void StartUserSession(this ISession session, int userId, string roleName, string fullName)
    {
        session.SetInt32(SessionKeys.UserId, userId);
        session.SetString(SessionKeys.RoleName, roleName);
        session.SetString(SessionKeys.FullName, fullName);
    }

    public static int? GetUserId(this ISession session)
    {
        return session.GetInt32(SessionKeys.UserId);
    }

    public static string? GetRoleName(this ISession session)
    {
        return session.GetString(SessionKeys.RoleName);
    }

    public static string? GetFullName(this ISession session)
    {
        return session.GetString(SessionKeys.FullName);
    }
}
