using Microsoft.Extensions.Options;
using RaceDayWeb.Infrastructure;
using RaceDayWeb.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ApiOptions>(builder.Configuration.GetSection(ApiOptions.SectionName));

// The MVC session holds who is logged in, their role, and their API session cookie.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "RaceDayWeb.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});
builder.Services.AddHttpContextAccessor();

// Every data operation goes through this typed client. The project has no database access of its own.
builder.Services.AddTransient<ApiSessionHandler>();
builder.Services.AddHttpClient<RaceDayApiClient>((services, client) =>
    {
        var api = services.GetRequiredService<IOptions<ApiOptions>>().Value;
        client.BaseAddress = new Uri(api.BaseUrl.TrimEnd('/') + "/");
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    // Cookies are added by ApiSessionHandler per user, so the shared handler must not keep its own cookie jar.
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseCookies = false })
    .AddHttpMessageHandler<ApiSessionHandler>();
builder.Services.AddSingleton<ImageUrls>();

// ApiErrorFilter turns API failures into friendly pages instead of raw errors.
builder.Services.AddControllersWithViews(options => options.Filters.Add<ApiErrorFilter>());

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

// A URL that matches no page shows the friendly Not Found page instead of a blank 404.
app.UseStatusCodePagesWithReExecute("/Home/Status/{0}");

app.UseRouting();
app.UseSession();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

// Lets the test project host the MVC app with WebApplicationFactory.
public partial class Program;
