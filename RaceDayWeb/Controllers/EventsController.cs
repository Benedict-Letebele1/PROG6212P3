using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Infrastructure;
using RaceDayWeb.Models;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers;

/// <summary>The public event page, and entering an event as a participant.</summary>
public class EventsController(RaceDayApiClient api, ImageUrls images) : Controller
{
    /// <summary>Full event information with its categories, from GET /api/events/{id}.</summary>
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var raceEvent = await api.GetEventAsync(id, ct);

        // A participant who has already entered sees their category and status instead of the entry form.
        EnrolmentResponse? mine = null;
        if (HttpContext.Session.CurrentUser()?.IsParticipant == true)
        {
            var enrolments = await api.GetMyEnrolmentsAsync(ct);
            mine = enrolments.FirstOrDefault(e => e.EventId == id && e.Status != EnrolmentStatuses.Cancelled)
                ?? enrolments.FirstOrDefault(e => e.EventId == id);
        }

        return View(new EventDetailsViewModel
        {
            Event = raceEvent,
            BannerUrl = images.Resolve(raceEvent.BannerImageUrl),
            MyEnrolment = mine
        });
    }

    /// <summary>Enters the logged-in participant in the chosen category through POST /api/events/{id}/enrol.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireRole(Roles.Participant)]
    public async Task<IActionResult> Enrol(int id, int? categoryId, CancellationToken ct)
    {
        if (categoryId is null)
        {
            this.FlashError("Choose a category to enter.");
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            var enrolment = await api.EnrolAsync(id, categoryId.Value, ct);
            this.FlashSuccess($"You're in! You entered {enrolment.EventName} in the {enrolment.CategoryName} category. Your entry is {enrolment.Status.ToLowerInvariant()}.");
        }
        catch (ApiException ex) when (ex.StatusCode is System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.Conflict)
        {
            // For example: already entered, or outside the category's age range on the event date.
            this.FlashError(ex.Message);
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}
