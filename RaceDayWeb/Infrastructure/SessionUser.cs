using System.Text.Json;
using RaceDayWeb.Services;

namespace RaceDayWeb.Infrastructure;

/// <summary>The logged-in user as the MVC app remembers them. It is stored in the server-side session as JSON.</summary>
public record SessionUser(int UserId, string Email, string FullName, string Role, string? OrganisationName)
{
    public bool IsOrganiser => Role == Roles.Organiser;

    public bool IsParticipant => Role == Roles.Participant;

    /// <summary>The first name, for the greeting in the navigation bar.</summary>
    public string FirstName => FullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? FullName;
}

/// <summary>Reads and writes the logged-in user in the session.</summary>
public static class SessionUserExtensions
{
    private const string Key = "CurrentUser";

    public static void SignIn(this ISession session, AuthResponse auth) =>
        session.SetString(Key, JsonSerializer.Serialize(new SessionUser(auth.UserId, auth.Email, auth.FullName, auth.Role, auth.OrganisationName)));

    /// <summary>Returns the logged-in user, or null for a guest.</summary>
    public static SessionUser? CurrentUser(this ISession session)
    {
        var json = session.GetString(Key);
        return json is null ? null : JsonSerializer.Deserialize<SessionUser>(json);
    }

    /// <summary>Updates the name and email shown in the menu after a profile change.</summary>
    public static void UpdateUser(this ISession session, ProfileResponse profile)
    {
        var user = session.CurrentUser();
        if (user is not null)
        {
            session.SetString(Key, JsonSerializer.Serialize(user with
            {
                FullName = profile.FullName,
                Email = profile.Email,
                OrganisationName = profile.OrganisationName
            }));
        }
    }

    /// <summary>Forgets the user and their API cookie.</summary>
    public static void SignOut(this ISession session) => session.Clear();
}
