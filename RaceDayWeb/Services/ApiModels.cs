namespace RaceDayWeb.Services;

// These records mirror the JSON the Part 2 API sends and receives. The MVC project does not reference
// the API project, so it has its own copies. They carry data only. Every rule stays in the API.

public record RegisterRequest(
    string Email,
    string Password,
    string FullName,
    string Role,
    string? OrganisationName,
    string? ContactNumber,
    DateOnly? DateOfBirth);

public record LoginRequest(string Email, string Password);

public record AuthResponse(int UserId, string Email, string FullName, string Role, string? OrganisationName);

public record ProfileResponse(
    int UserId,
    string Email,
    string FullName,
    string Role,
    string? OrganisationName,
    string? ContactNumber,
    DateOnly? DateOfBirth,
    string? ProfilePictureUrl);

public record UpdateProfileRequest(
    string FullName,
    string Email,
    string? OrganisationName,
    string? ContactNumber,
    DateOnly? DateOfBirth);

public record ImageUploadResponse(string ImageUrl);

public record EventRequest(
    string Name,
    string Description,
    DateTime Date,
    string Location,
    decimal Distance,
    string EventType,
    string? BannerImageUrl);

public record EventSummaryResponse(
    int EventId,
    string Name,
    DateTime Date,
    string Location,
    decimal Distance,
    string EventType,
    string? BannerImageUrl,
    string OrganisationName);

public record EventDetailResponse(
    int EventId,
    string Name,
    string Description,
    DateTime Date,
    string Location,
    decimal Distance,
    string EventType,
    string? BannerImageUrl,
    string OrganisationName,
    List<CategoryResponse> Categories);

public record OrganiserEventResponse(
    int EventId,
    string Name,
    DateTime Date,
    string Location,
    decimal Distance,
    string EventType,
    string? BannerImageUrl,
    int CategoryCount,
    int EnrolmentCount,
    int ConfirmedCount,
    int ResultCount);

public record CategoryRequest(string Name, string? Description, int? MinAge, int? MaxAge);

public record CategoryResponse(int CategoryId, int EventId, string Name, string? Description, int? MinAge, int? MaxAge);

public record EnrolRequest(int CategoryId);

public record StatusRequest(string Status);

public record EnrolmentResponse(
    int EnrolmentId,
    int EventId,
    string EventName,
    DateTime EventDate,
    int CategoryId,
    string CategoryName,
    string ParticipantName,
    string Status,
    DateTime RegistrationDate);

public record ResultRequest(int EnrolmentId, string FinishTime, int FinishPosition);

public record UpdateResultRequest(string FinishTime, int FinishPosition);

public record ResultResponse(
    int ResultId,
    int EnrolmentId,
    int EventId,
    string EventName,
    DateTime EventDate,
    string CategoryName,
    string ParticipantName,
    string FinishTime,
    int FinishPosition,
    string Status,
    int TotalFinishers);

/// <summary>Role names, event types and statuses, matching the values the API stores.</summary>
public static class Roles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}

public static class EventTypes
{
    public static readonly string[] All = ["Run", "Walk", "Cycle"];
}

public static class EnrolmentStatuses
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Cancelled = "Cancelled";
    public static readonly string[] All = [Pending, Confirmed, Cancelled];
}
