using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Infrastructure;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers;

/// <summary>
/// Shared by every organiser page. Only organisers get in, and the per-event pages load the event only when
/// it is one of the organiser's own events (from GET /api/events/mine). The API checks ownership again on
/// every change, so this check only keeps another organiser's event off the screen.
/// </summary>
[RequireRole(Roles.Organiser)]
public abstract class OrganiserControllerBase(RaceDayApiClient api) : Controller
{
    protected RaceDayApiClient Api { get; } = api;

    /// <summary>Returns the event with its categories, or null when the logged-in organiser does not own it.</summary>
    protected async Task<EventDetailResponse?> LoadOwnEventAsync(int eventId, CancellationToken ct)
    {
        var mine = await Api.GetMyEventsAsync(ct);
        if (mine.All(e => e.EventId != eventId))
        {
            return null;
        }

        var raceEvent = await Api.GetEventAsync(eventId, ct);
        ViewData["EventId"] = raceEvent.EventId;
        ViewData["EventName"] = raceEvent.Name;
        ViewData["EventDate"] = raceEvent.Date;
        return raceEvent;
    }

    /// <summary>The page shown when an organiser opens an event that belongs to someone else.</summary>
    protected IActionResult NotYourEvent() => RedirectToAction("AccessDenied", "Account");
}
