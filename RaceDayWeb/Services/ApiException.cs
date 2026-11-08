using System.Net;

namespace RaceDayWeb.Services;

/// <summary>Thrown when the API refuses a request. Message is the API's plain-language reason.</summary>
public class ApiException(HttpStatusCode statusCode, string message) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;

    /// <summary>The user's API session is gone (logged out, timed out, or the API restarted).</summary>
    public bool IsUnauthorized => StatusCode == HttpStatusCode.Unauthorized;

    public bool IsForbidden => StatusCode == HttpStatusCode.Forbidden;

    public bool IsNotFound => StatusCode == HttpStatusCode.NotFound;
}
