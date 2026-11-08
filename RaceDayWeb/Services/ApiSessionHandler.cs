using Microsoft.Extensions.Options;

namespace RaceDayWeb.Services;

/// <summary>
/// Carries each user's API session cookie. The API keeps the logged-in user and role in its own server-side
/// session. This handler stores the API's cookie in the MVC session after login and adds it to every later
/// request that user makes, so the API still enforces every role check from Part 2.
/// </summary>
public class ApiSessionHandler(IHttpContextAccessor httpContextAccessor, IOptions<ApiOptions> options) : DelegatingHandler
{
    public const string SessionKey = "ApiSessionCookie";

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var session = httpContextAccessor.HttpContext?.Session;
        var cookieName = options.Value.SessionCookieName;

        var apiCookie = session?.GetString(SessionKey);
        if (!string.IsNullOrEmpty(apiCookie))
        {
            request.Headers.Add("Cookie", $"{cookieName}={apiCookie}");
        }

        var response = await base.SendAsync(request, cancellationToken);

        // The API sends its cookie on login. Keep the newest value for this user.
        if (session is not null && response.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            foreach (var header in setCookies)
            {
                if (header.StartsWith($"{cookieName}=", StringComparison.Ordinal))
                {
                    var value = header[(cookieName.Length + 1)..].Split(';', 2)[0];
                    session.SetString(SessionKey, value);
                }
            }
        }

        return response;
    }
}
