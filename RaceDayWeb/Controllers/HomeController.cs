using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Models;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers;

/// <summary>The home page with all events and the friendly error pages.</summary>
public class HomeController(RaceDayApiClient api) : Controller
{
    /// <summary>Lists events, filtered by type, date and location through GET /api/events.</summary>
    public async Task<IActionResult> Index(string? type, DateOnly? date, string? location, CancellationToken ct)
    {
        var model = new EventListViewModel { Type = type, Date = date, Location = location };

        List<EventSummaryResponse> events;
        try
        {
            events = await api.GetEventsAsync(type, date, location, ct);
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            // An unknown type in the URL, for example. Show the message and all events instead.
            model.FilterError = ex.Message;
            model.Type = null;
            events = await api.GetEventsAsync(null, date, location, ct);
        }

        // The API returns events by date. Upcoming events are shown first, soonest first.
        var now = DateTime.Now;
        model.Upcoming = events.Where(e => e.Date >= now).ToList();
        model.Finished = events.Where(e => e.Date < now).OrderByDescending(e => e.Date).ToList();
        return View(model);
    }

    /// <summary>Shown by the exception handler outside Development, so no stack trace reaches the user.</summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View();
    }

    /// <summary>Shown for status codes such as 404 when a URL does not match any page.</summary>
    [Route("Home/Status/{code:int}")]
    public IActionResult Status(int code)
    {
        Response.StatusCode = code;
        return View(code == StatusCodes.Status404NotFound ? "NotFound" : "Error");
    }
}
