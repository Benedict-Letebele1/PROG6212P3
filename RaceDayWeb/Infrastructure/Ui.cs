using System.Globalization;

namespace RaceDayWeb.Infrastructure;

/// <summary>Small display helpers shared by the views, so every page formats things the same way.</summary>
public static class Ui
{
    private static readonly CultureInfo SouthAfrica = CultureInfo.GetCultureInfo("en-ZA");

    /// <summary>The CSS class for an enrolment status badge: green Confirmed, amber Pending, red Cancelled.</summary>
    public static string StatusClass(string status) => status switch
    {
        "Confirmed" => "status-confirmed",
        "Pending" => "status-pending",
        "Cancelled" => "status-cancelled",
        _ => "status-other"
    };

    /// <summary>The Bootstrap icon for an event type.</summary>
    public static string EventTypeIcon(string eventType) => eventType switch
    {
        "Run" => "bi-person-running",
        "Walk" => "bi-person-walking",
        "Cycle" => "bi-bicycle",
        _ => "bi-flag"
    };

    public static string EventTypeClass(string eventType) => $"type-{eventType.ToLowerInvariant()}";

    public static string Date(DateTime date) => date.ToString("ddd d MMM yyyy", SouthAfrica);

    public static string DateAndTime(DateTime date) => date.ToString("ddd d MMM yyyy, HH:mm", SouthAfrica);

    public static string Distance(decimal km) => $"{km.ToString("0.##", CultureInfo.InvariantCulture)} km";

    /// <summary>1st, 2nd, 3rd, 4th ... 11th, 12th, 13th, 21st.</summary>
    public static string Ordinal(int number)
    {
        var lastTwo = number % 100;
        var suffix = lastTwo is >= 11 and <= 13 ? "th" : (number % 10) switch
        {
            1 => "st",
            2 => "nd",
            3 => "rd",
            _ => "th"
        };
        return $"{number}{suffix}";
    }

    /// <summary>Age band text for a category, for example "18 to 39", "40+" or "All ages".</summary>
    public static string AgeBand(int? minAge, int? maxAge) => (minAge, maxAge) switch
    {
        (null, null) => "All ages",
        ({ } min, null) => $"{min}+",
        (null, { } max) => $"Up to {max}",
        ({ } min, { } max) => $"{min} to {max}"
    };

    /// <summary>Days until the event, for the dashboard countdown.</summary>
    public static string Countdown(DateTime eventDate, DateTime today)
    {
        var days = (eventDate.Date - today.Date).Days;
        return days switch
        {
            < 0 => "Finished",
            0 => "Today",
            1 => "Tomorrow",
            _ => $"In {days} days"
        };
    }
}
