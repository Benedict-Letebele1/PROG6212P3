using System.ComponentModel.DataAnnotations;
using RaceDayWeb.Services;

namespace RaceDayWeb.Models;

/// <summary>The organiser dashboard. The counts are added up from the API's figures for display only.</summary>
public class DashboardViewModel(List<OrganiserEventResponse> events, DateTime now)
{
    public DateTime Now { get; } = now;

    public List<OrganiserEventResponse> Events { get; } = events;

    public List<OrganiserEventResponse> Upcoming => Events.Where(e => e.Date >= Now).OrderBy(e => e.Date).ToList();

    public OrganiserEventResponse? NextEvent => Upcoming.FirstOrDefault();

    public int TotalEnrolments => Events.Sum(e => e.EnrolmentCount);

    public int TotalConfirmed => Events.Sum(e => e.ConfirmedCount);

    public int TotalResults => Events.Sum(e => e.ResultCount);

    /// <summary>The largest enrolment count, used to scale the bars in the table.</summary>
    public int MaxEnrolments => Events.Count == 0 ? 0 : Events.Max(e => e.EnrolmentCount);
}

/// <summary>The create and edit event form, including the optional banner image.</summary>
public class EventFormViewModel
{
    public int? EventId { get; set; }

    [Required(ErrorMessage = "Enter the event name.")]
    [StringLength(100, ErrorMessage = "Event name must be 100 characters or fewer.")]
    [Display(Name = "Event name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Describe the event.")]
    [StringLength(500, ErrorMessage = "Description must be 500 characters or fewer.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Choose the date and start time.")]
    [DataType(DataType.DateTime)]
    [Display(Name = "Date and start time")]
    public DateTime? Date { get; set; }

    [Required(ErrorMessage = "Enter the location.")]
    [StringLength(200, ErrorMessage = "Location must be 200 characters or fewer.")]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter the distance.")]
    [Range(0.01, 999.99, ErrorMessage = "Distance must be more than 0 km and no more than 999.99 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? Distance { get; set; }

    [Required(ErrorMessage = "Choose run, walk or cycle.")]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = "Run";

    [Display(Name = "Banner image")]
    public IFormFile? Banner { get; set; }

    /// <summary>The current banner, shown on the edit page. Not posted back.</summary>
    public string? CurrentBannerUrl { get; set; }

    public bool IsEdit => EventId is not null;
}

/// <summary>The add and edit category form.</summary>
public class CategoryFormViewModel : IValidatableObject
{
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "Enter the category name.")]
    [StringLength(50, ErrorMessage = "Category name must be 50 characters or fewer.")]
    [Display(Name = "Category name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "Description must be 200 characters or fewer.")]
    public string? Description { get; set; }

    [Range(0, 120, ErrorMessage = "Minimum age must be between 0 and 120.")]
    [Display(Name = "Minimum age")]
    public int? MinAge { get; set; }

    [Range(0, 120, ErrorMessage = "Maximum age must be between 0 and 120.")]
    [Display(Name = "Maximum age")]
    public int? MaxAge { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MinAge is not null && MaxAge is not null && MinAge > MaxAge)
        {
            yield return new ValidationResult("Minimum age can't be more than the maximum age.", [nameof(MaxAge)]);
        }
    }
}

/// <summary>The categories page: the event's categories plus the add form.</summary>
public class CategoriesPageViewModel
{
    public required EventDetailResponse Event { get; init; }

    public CategoryFormViewModel NewCategory { get; init; } = new();
}

/// <summary>The enrolments page for one event, with counts per status.</summary>
public class EventEnrolmentsViewModel
{
    public required EventDetailResponse Event { get; init; }

    public required List<EnrolmentResponse> Enrolments { get; init; }

    public string? StatusFilter { get; init; }

    public IEnumerable<EnrolmentResponse> Shown =>
        string.IsNullOrEmpty(StatusFilter) ? Enrolments : Enrolments.Where(e => e.Status == StatusFilter);

    public int Count(string status) => Enrolments.Count(e => e.Status == status);
}

/// <summary>One row on the results entry page: an enrolment and its result, if one has been captured.</summary>
public record ResultRow(EnrolmentResponse Enrolment, ResultResponse? Result);

/// <summary>The results entry page for one event.</summary>
public class EventResultsViewModel
{
    public required EventDetailResponse Event { get; init; }

    public required List<ResultRow> Rows { get; init; }

    public bool HasStarted => Event.Date <= DateTime.Now;

    public int Captured => Rows.Count(r => r.Result is not null);
}
