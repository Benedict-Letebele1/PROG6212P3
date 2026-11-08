using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using RaceDayWeb.Services;

namespace RaceDayWeb.Tests.Infrastructure;

/// <summary>Hosts the MVC app in memory, with its API client pointed at <see cref="FakeApiHandler"/>.</summary>
public partial class RaceDayWebFactory : WebApplicationFactory<Program>
{
    public FakeApiHandler Api { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Api:BaseUrl", "http://fake-api");
        builder.UseSetting("Api:PublicUrl", "http://localhost:5088");
        builder.ConfigureTestServices(services =>
            services.AddHttpClient<RaceDayApiClient>().ConfigurePrimaryHttpMessageHandler(() => Api));
    }

    /// <summary>A browser-like client that keeps cookies but does not follow redirects, so tests can check them.</summary>
    public HttpClient Browser() => CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

    /// <summary>Logs the browser in through the real login form. The fake API answers as the given role.</summary>
    public async Task LoginAsync(HttpClient browser, string role, string name = "Jarred Tester")
    {
        Api.On(HttpMethod.Post, "/api/auth/login", HttpStatusCode.OK,
            new AuthResponse(7, "jarred@test.co.za", name, role, role == Roles.Organiser ? "Jarred Events" : null),
            setCookie: "RaceDayApi.Session=fake-session-123; path=/; httponly");

        var token = await TokenAsync(browser, "/Account/Login");
        var response = await browser.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = "jarred@test.co.za",
            ["Password"] = "Password123!",
            ["__RequestVerificationToken"] = token
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    /// <summary>Reads the anti-forgery token from a page's form.</summary>
    public static async Task<string> TokenAsync(HttpClient browser, string url)
    {
        var html = await browser.GetStringAsync(url);
        return TokenPattern().Match(html).Groups[1].Value;
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")]
    private static partial Regex TokenPattern();
}
