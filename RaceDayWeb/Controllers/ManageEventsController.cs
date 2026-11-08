using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Infrastructure;
using RaceDayWeb.Models;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers;

/// <summary>Create, edit and delete the organiser's own events, including the banner image.</summary>
public class ManageEventsController(RaceDayApiClient api, ImageUrls images) : OrganiserControllerBase(api)
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var events = await Api.GetMyEventsAsync(ct);
        ViewData["Images"] = images;
        return View(events.OrderBy(e => e.Date < DateTime.Now).ThenBy(e => e.Date).ToList());
    }

    [HttpGet]
    public IActionResult Create()
    {
        // A sensible default: a Saturday morning start a month from now.
        var start = DateTime.Today.AddDays(30);
        start = start.AddDays(((int)DayOfWeek.Saturday - (int)start.DayOfWeek + 7) % 7).AddHours(7);
        return View("Form", new EventFormViewModel { Date = start });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventFormViewModel model, CancellationToken ct)
    {
        model.EventId = null;
        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        EventDetailResponse created;
        try
        {
            created = await Api.CreateEventAsync(ToRequest(model, bannerUrl: null), ct);
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Form", model);
        }

        // The banner is uploaded after the event exists, because the upload endpoint needs the event ID.
        if (!await TryUploadBannerAsync(created.EventId, model.Banner, ct))
        {
            return RedirectToAction(nameof(Edit), new { id = created.EventId });
        }

        this.FlashSuccess($"{created.Name} has been created. Add categories so participants can enter.");
        return RedirectToAction("Index", "Categories", new { eventId = created.EventId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var raceEvent = await LoadOwnEventAsync(id, ct);
        if (raceEvent is null)
        {
            return NotYourEvent();
        }

        return View("Form", new EventFormViewModel
        {
            EventId = raceEvent.EventId,
            Name = raceEvent.Name,
            Description = raceEvent.Description,
            Date = raceEvent.Date,
            Location = raceEvent.Location,
            Distance = raceEvent.Distance,
            EventType = raceEvent.EventType,
            CurrentBannerUrl = images.Resolve(raceEvent.BannerImageUrl)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EventFormViewModel model, CancellationToken ct)
    {
        var current = await LoadOwnEventAsync(id, ct);
        if (current is null)
        {
            return NotYourEvent();
        }

        model.EventId = id;
        model.CurrentBannerUrl = images.Resolve(current.BannerImageUrl);
        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        try
        {
            // The existing banner URL comes from the API, not the form, so it is kept unless a new file is uploaded.
            await Api.UpdateEventAsync(id, ToRequest(model, current.BannerImageUrl), ct);
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Form", model);
        }

        if (!await TryUploadBannerAsync(id, model.Banner, ct))
        {
            return RedirectToAction(nameof(Edit), new { id });
        }

        this.FlashSuccess($"{model.Name} has been updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var raceEvent = await LoadOwnEventAsync(id, ct);
        if (raceEvent is null)
        {
            return NotYourEvent();
        }

        var mine = await Api.GetMyEventsAsync(ct);
        ViewData["EnrolmentCount"] = mine.Single(e => e.EventId == id).EnrolmentCount;
        return View(raceEvent);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken ct)
    {
        try
        {
            await Api.DeleteEventAsync(id, ct);
            this.FlashSuccess("The event has been deleted.");
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            // The API refuses to delete an event that has enrolments, so no participant records are lost.
            this.FlashError(ex.Message);
        }

        return RedirectToAction(nameof(Index));
    }

    private static EventRequest ToRequest(EventFormViewModel model, string? bannerUrl) =>
        new(model.Name.Trim(), model.Description.Trim(), model.Date!.Value, model.Location.Trim(), model.Distance!.Value, model.EventType, bannerUrl);

    /// <summary>
    /// Sends the banner to POST /api/events/{id}/upload-banner. The API stores it in Azure Blob Storage and saves
    /// the URL on the event. Returns false (with a message) when the API rejects the file.
    /// </summary>
    private async Task<bool> TryUploadBannerAsync(int eventId, IFormFile? banner, CancellationToken ct)
    {
        if (banner is null || banner.Length == 0)
        {
            return true;
        }

        try
        {
            await Api.UploadBannerAsync(eventId, banner, ct);
            return true;
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            this.FlashError($"The event was saved, but the banner wasn't uploaded: {ex.Message}");
            return false;
        }
    }
}
