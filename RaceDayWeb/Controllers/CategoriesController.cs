using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Infrastructure;
using RaceDayWeb.Models;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers;

/// <summary>Add, edit and delete the age or distance categories of one of the organiser's events.</summary>
[Route("ManageEvents/{eventId:int}/Categories/{action=Index}")]
public class CategoriesController(RaceDayApiClient api) : OrganiserControllerBase(api)
{
    public async Task<IActionResult> Index(int eventId, CancellationToken ct)
    {
        var raceEvent = await LoadOwnEventAsync(eventId, ct);
        return raceEvent is null ? NotYourEvent() : View(new CategoriesPageViewModel { Event = raceEvent });
    }

    /// <summary>Adds a category through POST /api/events/{eventId}/categories.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int eventId, [Bind(Prefix = nameof(CategoriesPageViewModel.NewCategory))] CategoryFormViewModel form, CancellationToken ct)
    {
        var raceEvent = await LoadOwnEventAsync(eventId, ct);
        if (raceEvent is null)
        {
            return NotYourEvent();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var added = await Api.AddCategoryAsync(eventId, ToRequest(form), ct);
                this.FlashSuccess($"Added the {added.Name} category.");
                return RedirectToAction(nameof(Index), new { eventId });
            }
            catch (ApiException ex) when (ex.StatusCode is System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.Conflict)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
        }

        return View(nameof(Index), new CategoriesPageViewModel { Event = raceEvent, NewCategory = form });
    }

    /// <summary>Changes a category through PUT /api/categories/{id}.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int eventId, CategoryFormViewModel form, CancellationToken ct)
    {
        if (!ModelState.IsValid || form.CategoryId is null)
        {
            this.FlashError(ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Check the category details.");
            return RedirectToAction(nameof(Index), new { eventId });
        }

        try
        {
            var updated = await Api.UpdateCategoryAsync(form.CategoryId.Value, ToRequest(form), ct);
            this.FlashSuccess($"Updated the {updated.Name} category.");
        }
        catch (ApiException ex) when (ex.StatusCode is System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.Conflict)
        {
            this.FlashError(ex.Message);
        }

        return RedirectToAction(nameof(Index), new { eventId });
    }

    /// <summary>Deletes a category through DELETE /api/categories/{id}. The API refuses when it has enrolments.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int eventId, int categoryId, CancellationToken ct)
    {
        try
        {
            await Api.DeleteCategoryAsync(categoryId, ct);
            this.FlashSuccess("The category has been deleted.");
        }
        catch (ApiException ex) when (ex.StatusCode is System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.Conflict)
        {
            this.FlashError(ex.Message);
        }

        return RedirectToAction(nameof(Index), new { eventId });
    }

    private static CategoryRequest ToRequest(CategoryFormViewModel form) =>
        new(form.Name.Trim(), string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim(), form.MinAge, form.MaxAge);
}
