namespace RaceDayApi.Contracts;

/// <summary>
/// Request body for POST /api/events/{eventId}/results. FinishTime is HH:mm:ss and FinishPosition starts at 1.
/// </summary>
public record ResultRequest(int EnrolmentId, string FinishTime, int FinishPosition);

/// <summary>Request body for PUT /api/results/{id}. FinishTime is HH:mm:ss and FinishPosition starts at 1.</summary>
public record UpdateResultRequest(string FinishTime, int FinishPosition);

/// <summary>
/// A recorded result for one enrolment. TotalFinishers is the number of results recorded for the same event,
/// so a page can show "47th out of 312".
/// </summary>
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
