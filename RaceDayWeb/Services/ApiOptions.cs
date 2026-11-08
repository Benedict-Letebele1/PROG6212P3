namespace RaceDayWeb.Services;

/// <summary>Settings for reaching the Part 2 API, read from the "Api" section of appsettings.json.</summary>
public class ApiOptions
{
    public const string SectionName = "Api";

    /// <summary>The address this app uses to call the API, for example http://localhost:5088.</summary>
    public string BaseUrl { get; set; } = "http://localhost:5088";

    /// <summary>
    /// The address the browser uses to load images the API keeps on its own disk. It differs from BaseUrl in
    /// Docker, where the app calls host.docker.internal but the browser still sees localhost. Azure image URLs
    /// are already absolute and are used as they are.
    /// </summary>
    public string? PublicUrl { get; set; }

    /// <summary>The name of the API's session cookie.</summary>
    public string SessionCookieName { get; set; } = "RaceDayApi.Session";
}
