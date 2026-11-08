using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Models;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers;

/// <summary>The organiser dashboard.</summary>
public class OrganiserController(RaceDayApiClient api) : OrganiserControllerBase(api)
{
    /// <summary>A summary of every event the organiser created, with enrolments per event and upcoming dates.</summary>
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        var events = await Api.GetMyEventsAsync(ct);
        return View(new DashboardViewModel(events, DateTime.Now));
    }
}
