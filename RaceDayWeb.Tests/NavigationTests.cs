using System.Net;
using RaceDayWeb.Services;
using RaceDayWeb.Tests.Infrastructure;

namespace RaceDayWeb.Tests;

/// <summary>Each role gets its own menu, and protected pages send guests and the wrong role away.</summary>
public class NavigationTests : IDisposable
{
    private readonly RaceDayWebFactory _factory = new();

    public NavigationTests()
    {
        _factory.Api.On(HttpMethod.Get, "/api/events", HttpStatusCode.OK, new List<EventSummaryResponse>());
        _factory.Api.On(HttpMethod.Get, "/api/events/mine", HttpStatusCode.OK, new List<OrganiserEventResponse>());
    }

    [Theory]
    [InlineData("/Participant/MyEvents")]
    [InlineData("/Participant/MyResults")]
    [InlineData("/Organiser/Dashboard")]
    [InlineData("/ManageEvents")]
    [InlineData("/Profile")]
    public async Task Guest_OpeningAProtectedPage_IsSentToLogin_WithAReturnUrl(string url)
    {
        var response = await _factory.Browser().GetAsync(url);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/Account/Login", response.Headers.Location!.OriginalString);
        Assert.Contains(Uri.EscapeDataString(url), response.Headers.Location.OriginalString);
    }

    [Fact]
    public async Task Participant_OpeningAnOrganiserPage_GetsAccessDenied()
    {
        var browser = _factory.Browser();
        await _factory.LoginAsync(browser, Roles.Participant);

        var response = await browser.GetAsync("/Organiser/Dashboard");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Account/AccessDenied", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Organiser_OpeningAParticipantPage_GetsAccessDenied()
    {
        var browser = _factory.Browser();
        await _factory.LoginAsync(browser, Roles.Organiser);

        var response = await browser.GetAsync("/Participant/MyResults");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Account/AccessDenied", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Guest_SeesLoginAndJoin_InTheMenu()
    {
        var html = await _factory.Browser().GetStringAsync("/");

        Assert.Contains("Join RaceDay", html);
        Assert.DoesNotContain("My enrolments", html);
        Assert.DoesNotContain("Dashboard", html);
    }

    [Fact]
    public async Task Participant_SeesParticipantMenu()
    {
        var browser = _factory.Browser();
        await _factory.LoginAsync(browser, Roles.Participant);

        var html = await browser.GetStringAsync("/");

        Assert.Contains("My enrolments", html);
        Assert.Contains("My results", html);
        Assert.DoesNotContain("Dashboard", html);
        Assert.Contains("Log out", html);
    }

    [Fact]
    public async Task Organiser_SeesOrganiserMenu()
    {
        var browser = _factory.Browser();
        await _factory.LoginAsync(browser, Roles.Organiser);

        var html = await browser.GetStringAsync("/Organiser/Dashboard");

        Assert.Contains("Dashboard", html);
        Assert.Contains("My events", html);
        Assert.DoesNotContain("My enrolments", html);
    }

    [Fact]
    public async Task Login_SendsTheApiSessionCookie_OnLaterApiCalls()
    {
        var browser = _factory.Browser();
        await _factory.LoginAsync(browser, Roles.Organiser);

        await browser.GetStringAsync("/Organiser/Dashboard");

        var call = _factory.Api.Requests.Last(r => r.Path == "/api/events/mine");
        Assert.Equal("RaceDayApi.Session=fake-session-123", call.Cookie);
    }

    [Fact]
    public async Task Logout_ClearsTheSession_AndProtectedPagesNeedLoginAgain()
    {
        var browser = _factory.Browser();
        await _factory.LoginAsync(browser, Roles.Participant);
        _factory.Api.On(HttpMethod.Post, "/api/auth/logout", HttpStatusCode.OK, new { message = "You have been logged out." });

        var token = await RaceDayWebFactory.TokenAsync(browser, "/");
        var logout = await browser.PostAsync("/Account/Logout", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token
        }));
        var afterwards = await browser.GetAsync("/Participant/MyEvents");

        Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
        Assert.Contains(_factory.Api.Requests, r => r.Path == "/api/auth/logout");
        Assert.StartsWith("/Account/Login", afterwards.Headers.Location!.OriginalString);
    }

    public void Dispose() => _factory.Dispose();
}
