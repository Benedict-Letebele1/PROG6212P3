using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Infrastructure;
using RaceDayWeb.Models;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers;

/// <summary>Register, log in and log out. Each one goes through the API's /api/auth endpoints.</summary>
public class AccountController(RaceDayApiClient api) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl)
    {
        if (HttpContext.Session.CurrentUser() is { } user)
        {
            return HomeFor(user);
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await SignInAsync(model.Email, model.Password, ct);
        }
        catch (ApiException ex) when (ex.IsUnauthorized || ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            // The API says "Invalid email or password." without saying which, and so do we.
            ModelState.AddModelError(string.Empty, ex.Message);
            model.Password = string.Empty;
            return View(model);
        }

        var user = HttpContext.Session.CurrentUser()!;
        this.FlashSuccess($"Welcome back, {user.FirstName}.");

        // Only local URLs are followed, so the login page cannot be used to send someone to another site.
        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return LocalRedirect(model.ReturnUrl);
        }

        return HomeFor(user);
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (HttpContext.Session.CurrentUser() is { } user)
        {
            return HomeFor(user);
        }

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var isOrganiser = model.Role == Roles.Organiser;
        try
        {
            await api.RegisterAsync(new RegisterRequest(
                model.Email.Trim(),
                model.Password,
                model.FullName.Trim(),
                model.Role,
                isOrganiser ? model.OrganisationName?.Trim() : null,
                isOrganiser ? model.ContactNumber?.Trim() : null,
                isOrganiser ? null : model.DateOfBirth), ct);
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }

        // Registering does not start a session in the API, so log the new user straight in.
        await SignInAsync(model.Email, model.Password, ct);
        var user = HttpContext.Session.CurrentUser()!;
        this.FlashSuccess(isOrganiser
            ? $"Welcome to RaceDay, {user.FirstName}. Create your first event to get started."
            : $"Welcome to RaceDay, {user.FirstName}. Find an event and enter.");

        return HomeFor(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        if (HttpContext.Session.CurrentUser() is not null)
        {
            try
            {
                await api.LogoutAsync(ct);
            }
            catch (Exception ex) when (ex is ApiException or HttpRequestException)
            {
                // The API session may already be gone. The MVC session is cleared either way.
            }
        }

        HttpContext.Session.SignOut();
        this.FlashSuccess("You have been logged out. See you at the start line.");
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }

    private async Task SignInAsync(string email, string password, CancellationToken ct)
    {
        // Start clean, so an old API cookie is never sent with the new login.
        HttpContext.Session.SignOut();
        var auth = await api.LoginAsync(new LoginRequest(email.Trim(), password), ct);
        HttpContext.Session.SignIn(auth);
    }

    /// <summary>Organisers start on their dashboard, participants on the event list.</summary>
    private RedirectToActionResult HomeFor(SessionUser user) =>
        user.IsOrganiser ? RedirectToAction("Dashboard", "Organiser") : RedirectToAction("Index", "Home");
}
