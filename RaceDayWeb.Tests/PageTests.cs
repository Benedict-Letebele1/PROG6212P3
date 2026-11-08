using System.Net;
using RaceDayWeb.Services;
using RaceDayWeb.Tests.Infrastructure;

namespace RaceDayWeb.Tests;

/// <summary>The pages show the API's data correctly and fail gracefully instead of showing raw errors.</summary>
public class PageTests : IDisposable
{
    private readonly RaceDayWebFactory _factory = new();

    [Fact]
    public async Task MyEvents_ColourCodesEachStatus()
    {
        var browser = _factory.Browser();
        await _factory.LoginAsync(browser, Roles.Participant);
        var future = DateTime.Now.AddDays(10);
        _factory.Api.On(HttpMethod.Get, "/api/enrolments", HttpStatusCode.OK, new List<EnrolmentResponse>
        {
            new(1, 1, "Jarred Confirmed Run", future, 1, "Open", "Jarred Tester", "Confirmed", DateTime.Now),
            new(2, 2, "Jarred Pending Walk", future, 2, "Open", "Jarred Tester", "Pending", DateTime.Now),
            new(3, 3, "Jarred Cancelled Ride", future, 3, "Open", "Jarred Tester", "Cancelled", DateTime.Now)
        });

        var html = await browser.GetStringAsync("/Participant/MyEvents");

        Assert.Contains("Jarred Confirmed Run", html);
        Assert.Contains("rd-status status-confirmed", html);
        Assert.Contains("rd-status status-pending", html);
        Assert.Contains("rd-status status-cancelled", html);
    }

    [Fact]
    public async Task MyResults_ShowsThePositionOutOfTheFinishers()
    {
        var browser = _factory.Browser();
        await _factory.LoginAsync(browser, Roles.Participant);
        _factory.Api.On(HttpMethod.Get, "/api/results/me", HttpStatusCode.OK, new List<ResultResponse>
        {
            new(1, 1, 1, "Jarred City Marathon", new DateTime(2026, 8, 16), "Senior", "Jarred Tester", "03:28:45", 47, "Published", 312)
        });

        var html = await browser.GetStringAsync("/Participant/MyResults");

        Assert.Contains("47th", html);
        Assert.Contains("out of 312 finishers", html);
        Assert.Contains("03:28:45", html);
        Assert.Contains("Senior", html);
    }

    [Fact]
    public async Task EventDetails_UsesTheAzureBannerUrl_AsItIs()
    {
        const string blobUrl = "https://racedayst.blob.core.windows.net/raceday-images/banners/jarred.png";
        _factory.Api.On(HttpMethod.Get, "/api/events/5", HttpStatusCode.OK, new EventDetailResponse(
            5, "Jarred Night Run", "A night run", DateTime.Now.AddDays(30), "Pretoria", 10m, "Run", blobUrl, "Jarred Events",
            [new CategoryResponse(9, 5, "Open", null, 18, null)]));

        var html = await _factory.Browser().GetStringAsync("/Events/Details/5");

        Assert.Contains(blobUrl, html);
        Assert.Contains("Log in as a participant to enter", html);
    }

    [Fact]
    public void ImageUrls_AddTheApiAddress_ToLocalPaths_Only()
    {
        var images = new ImageUrls(Microsoft.Extensions.Options.Options.Create(new ApiOptions
        {
            BaseUrl = "http://host.docker.internal:5088",
            PublicUrl = "http://localhost:5088"
        }));

        Assert.Equal("http://localhost:5088/uploads/banners/a.png", images.Resolve("/uploads/banners/a.png"));
        Assert.Equal("https://x.blob.core.windows.net/c/a.png", images.Resolve("https://x.blob.core.windows.net/c/a.png"));
        Assert.Null(images.Resolve(null));
    }

    [Fact]
    public async Task ApiDown_ShowsAFriendlyPage_NotAStackTrace()
    {
        _factory.Api.ApiIsDown = true;

        var response = await _factory.Browser().GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("RaceDay is taking a breather", html);
        Assert.DoesNotContain("HttpRequestException", html);
    }

    [Fact]
    public async Task ExpiredApiSession_LogsTheUserOut_AndAsksThemToLogInAgain()
    {
        var browser = _factory.Browser();
        await _factory.LoginAsync(browser, Roles.Participant);
        _factory.Api.On(HttpMethod.Get, "/api/enrolments", HttpStatusCode.Unauthorized, new { message = "You need to log in first." });

        var response = await browser.GetAsync("/Participant/MyEvents");
        var again = await browser.GetAsync("/Participant/MyResults");

        Assert.StartsWith("/Account/Login", response.Headers.Location!.OriginalString);
        Assert.StartsWith("/Account/Login", again.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Enrol_ShowsTheApiReason_WhenTheEntryIsRefused()
    {
        var browser = _factory.Browser();
        await _factory.LoginAsync(browser, Roles.Participant);
        _factory.Api.On(HttpMethod.Get, "/api/events/5", HttpStatusCode.OK, new EventDetailResponse(
            5, "Jarred Night Run", "A night run", DateTime.Now.AddDays(30), "Pretoria", 10m, "Run", null, "Jarred Events",
            [new CategoryResponse(9, 5, "Junior", null, null, 17)]));
        _factory.Api.On(HttpMethod.Get, "/api/enrolments", HttpStatusCode.OK, new List<EnrolmentResponse>());
        _factory.Api.On(HttpMethod.Post, "/api/events/5/enrol", HttpStatusCode.BadRequest,
            new { message = "Your age on the event date is outside this category's age range." });

        var token = await RaceDayWebFactory.TokenAsync(browser, "/Events/Details/5");
        await browser.PostAsync("/Events/Enrol/5", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["categoryId"] = "9",
            ["__RequestVerificationToken"] = token
        }));
        var html = await browser.GetStringAsync("/Events/Details/5");

        Assert.Contains("outside this category&#x27;s age range", html);
    }

    [Fact]
    public async Task UnknownPage_ShowsTheFriendlyNotFoundPage()
    {
        var response = await _factory.Browser().GetAsync("/this/page/does/not/exist");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("Wrong turn", html);
    }

    public void Dispose() => _factory.Dispose();
}
