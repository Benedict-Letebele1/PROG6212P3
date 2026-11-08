using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Infrastructure;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers;

/// <summary>The participant's own enrolments and race history. Organisers and guests cannot open these pages.</summary>
[RequireRole(Roles.Participant)]
public class ParticipantController(RaceDayApiClient api) : Controller
{
    /// <summary>Every event the participant entered, with its category and status, from GET /api/enrolments.</summary>
    public async Task<IActionResult> MyEvents(CancellationToken ct)
    {
        var enrolments = await api.GetMyEnrolmentsAsync(ct);
        return View(enrolments.OrderBy(e => e.EventDate < DateTime.Now).ThenBy(e => e.EventDate).ToList());
    }

    /// <summary>The participant's finish times and positions, from GET /api/results/me.</summary>
    public async Task<IActionResult> MyResults(CancellationToken ct)
    {
        var results = await api.GetMyResultsAsync(ct);
        return View(results);
    }
}
