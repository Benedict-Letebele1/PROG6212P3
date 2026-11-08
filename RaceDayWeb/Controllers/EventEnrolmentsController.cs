using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Infrastructure;
using RaceDayWeb.Models;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers;

/// <summary>Every participant enrolled in one of the organiser's events, with their category and status.</summary>
[Route("ManageEvents/{eventId:int}/Enrolments/{action=Index}")]
public class EventEnrolmentsController(RaceDayApiClient api) : OrganiserControllerBase(api)
{
    /// <summary>Lists the enrolments from GET /api/events/{eventId}/enrolments, optionally filtered by status.</summary>
    public async Task<IActionResult> Index(int eventId, string? status, CancellationToken ct)
    {
        var raceEvent = await LoadOwnEventAsync(eventId, ct);
        if (raceEvent is null)
        {
            return NotYourEvent();
        }

        var enrolments = await Api.GetEventEnrolmentsAsync(eventId, ct);
        return View(new EventEnrolmentsViewModel
        {
            Event = raceEvent,
            Enrolments = enrolments.OrderBy(e => e.CategoryName).ThenBy(e => e.ParticipantName).ToList(),
            StatusFilter = EnrolmentStatuses.All.Contains(status) ? status : null
        });
    }

    /// <summary>Sets an enrolment to Pending, Confirmed or Cancelled through PUT /api/enrolments/{id}/status.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int eventId, int enrolmentId, string status, string? filter, CancellationToken ct)
    {
        try
        {
            var updated = await Api.SetEnrolmentStatusAsync(enrolmentId, status, ct);
            this.FlashSuccess($"{updated.ParticipantName} is now {updated.Status.ToLowerInvariant()}.");
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            this.FlashError(ex.Message);
        }

        return RedirectToAction(nameof(Index), new { eventId, status = filter });
    }
}
