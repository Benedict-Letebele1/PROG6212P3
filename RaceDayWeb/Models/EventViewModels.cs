using RaceDayWeb.Services;

namespace RaceDayWeb.Models;

/// <summary>The home page: filter values and the matching events split into upcoming and finished.</summary>
public class EventListViewModel
{
    public string? Type { get; set; }

    public DateOnly? Date { get; set; }

    public string? Location { get; set; }

    public List<EventSummaryResponse> Upcoming { get; set; } = [];

    public List<EventSummaryResponse> Finished { get; set; } = [];

    /// <summary>Set when the API rejected a filter, so the page can show why.</summary>
    public string? FilterError { get; set; }

    public bool IsFiltered => !string.IsNullOrWhiteSpace(Type) || Date is not null || !string.IsNullOrWhiteSpace(Location);
}

/// <summary>The event details page, with the logged-in participant's enrolment if they have one.</summary>
public class EventDetailsViewModel
{
    public required EventDetailResponse Event { get; init; }

    public string? BannerUrl { get; init; }

    public EnrolmentResponse? MyEnrolment { get; init; }

    public bool IsFinished => Event.Date < DateTime.Now;

    public int? SelectedCategoryId { get; set; }
}
