using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Infrastructure;
using RaceDayWeb.Models;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers;

/// <summary>View and update your own profile. Participants can also upload a profile picture.</summary>
[RequireRole]
public class ProfileController(RaceDayApiClient api, ImageUrls images) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var profile = await api.GetProfileAsync(ct);
        return View(ProfileViewModel.From(profile, images.Resolve(profile.ProfilePictureUrl)));
    }

    /// <summary>Saves the details through PUT /api/users/profile.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ProfileViewModel model, CancellationToken ct)
    {
        // The role comes from the session, never from the form.
        var current = await api.GetProfileAsync(ct);
        model.Role = current.Role;
        model.ProfilePictureUrl = images.Resolve(current.ProfilePictureUrl);
        ModelState.Clear();
        TryValidateModel(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var updated = await api.UpdateProfileAsync(new UpdateProfileRequest(
                model.FullName.Trim(),
                model.Email.Trim(),
                model.IsOrganiser ? model.OrganisationName?.Trim() : null,
                model.IsOrganiser ? model.ContactNumber?.Trim() : null,
                model.IsOrganiser ? null : model.DateOfBirth), ct);
            HttpContext.Session.UpdateUser(updated);
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }

        this.FlashSuccess("Your profile has been updated.");
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Sends the picture to POST /api/users/profile/picture. The API stores it in Azure Blob Storage and saves
    /// the URL. The MVC app never talks to Azure itself.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireRole(Roles.Participant)]
    public async Task<IActionResult> Picture(IFormFile? picture, CancellationToken ct)
    {
        if (picture is null || picture.Length == 0)
        {
            this.FlashError("Choose a picture to upload.");
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await api.UploadProfilePictureAsync(picture, ct);
            this.FlashSuccess("Your new profile picture is up.");
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            // The API checks the size and type, so its message (for example "must be 2 MB or smaller") is shown.
            this.FlashError(ex.Message);
        }

        return RedirectToAction(nameof(Index));
    }
}
