using System.Net;
using System.Text;
using System.Text.Json;

namespace RaceDayWeb.Tests.Infrastructure;

/// <summary>
/// Stands in for the Part 2 API, so the MVC tests run without the API, SQL Server or Azure. Each test registers
/// the responses it needs by method and path. Every request is recorded, so a test can check what was sent.
/// </summary>
public class FakeApiHandler : HttpMessageHandler
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>> _routes = new(StringComparer.OrdinalIgnoreCase);

    public List<(HttpMethod Method, string Path, string? Cookie)> Requests { get; } = [];

    /// <summary>When true every call fails as if the API were not running.</summary>
    public bool ApiIsDown { get; set; }

    public void On(HttpMethod method, string path, HttpStatusCode status, object? body = null, string? setCookie = null) =>
        _routes[$"{method} {path}"] = _ =>
        {
            var response = new HttpResponseMessage(status);
            if (body is not null)
            {
                response.Content = new StringContent(JsonSerializer.Serialize(body, Json), Encoding.UTF8, "application/json");
            }

            if (setCookie is not null)
            {
                response.Headers.Add("Set-Cookie", setCookie);
            }

            return response;
        };

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri!.AbsolutePath;
        var cookie = request.Headers.TryGetValues("Cookie", out var values) ? string.Join("; ", values) : null;
        Requests.Add((request.Method, path, cookie));

        if (ApiIsDown)
        {
            throw new HttpRequestException("Connection refused");
        }

        return Task.FromResult(_routes.TryGetValue($"{request.Method} {path}", out var route)
            ? route(request)
            : new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("{\"message\":\"Not found in the fake API.\"}", Encoding.UTF8, "application/json")
            });
    }
}
