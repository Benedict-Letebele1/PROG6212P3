using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Infrastructure;
using RaceDayWeb.Models;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers;

/// <summary>Capture and publish finish times and positions for the participants of an event.</summary>
[Route("ManageEvents/{eventId:int}/Results/{action=Index}")]
public class EventResultsController(RaceDayApiClient api) : OrganiserControllerBase(api)
{
    /// <summary>One row per active enrolment, with its result if one has been captured.</summary>
    public async Task<IActionResult> Index(int eventId, CancellationToken ct)
    {
        var raceEvent = await LoadOwnEventAsync(eventId, ct);
        if (raceEvent is null)
        {
            return NotYourEvent();
        }

        var enrolments = await Api.GetEventEnrolmentsAsync(eventId, ct);
        var results = (await Api.GetEventResultsAsync(eventId, ct)).ToDictionary(r => r.EnrolmentId);

        var rows = enrolments
            .Where(e => e.Status != EnrolmentStatuses.Cancelled || results.ContainsKey(e.EnrolmentId))
            .Select(e => new ResultRow(e, results.GetValueOrDefault(e.EnrolmentId)))
            // Captured results first in finishing order, then the participants still waiting for a result.
            .OrderBy(r => r.Result is null)
            .ThenBy(r => r.Result?.FinishPosition)
            .ThenBy(r => r.Enrolment.ParticipantName)
            .ToList();

        return View(new EventResultsViewModel { Event = raceEvent, Rows = rows });
    }

    /// <summary>
    /// Saves one participant's result. A new result goes to POST /api/events/{eventId}/results, and a change
    /// goes to PUT /api/results/{id}. The API checks the time format, the position and the enrolment.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(int eventId, int enrolmentId, int? resultId, string? finishTime, int? finishPosition, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(finishTime) || finishPosition is null)
        {
            this.FlashError("Enter both a finish time (hh:mm:ss) and a position.");
            return RedirectToAction(nameof(Index), new { eventId });
        }

        try
        {
            var saved = resultId is null
                ? await Api.CaptureResultAsync(eventId, new ResultRequest(enrolmentId, finishTime.Trim(), finishPosition.Value), ct)
                : await Api.UpdateResultAsync(resultId.Value, new UpdateResultRequest(finishTime.Trim(), finishPosition.Value), ct);

            this.FlashSuccess($"Saved {saved.ParticipantName}: {Ui.Ordinal(saved.FinishPosition)} in {saved.FinishTime}.");
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            this.FlashError(ex.Message);
        }

        return RedirectToAction(nameof(Index), new { eventId });
    }
}
