using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDayWeb.Services;

/// <summary>
/// The only way the MVC app reaches data. There is one method per API endpoint, and each one either returns
/// the API's response or throws <see cref="ApiException"/> with the API's error message.
/// </summary>
public class RaceDayApiClient(HttpClient http)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // Authentication
    public Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default) =>
        SendAsync<AuthResponse>(HttpMethod.Post, "api/auth/register", request, ct);

    public Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default) =>
        SendAsync<AuthResponse>(HttpMethod.Post, "api/auth/login", request, ct);

    public Task LogoutAsync(CancellationToken ct = default) =>
        SendAsync(HttpMethod.Post, "api/auth/logout", null, ct);

    // Profile
    public Task<ProfileResponse> GetProfileAsync(CancellationToken ct = default) =>
        SendAsync<ProfileResponse>(HttpMethod.Get, "api/users/profile", null, ct);

    public Task<ProfileResponse> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct = default) =>
        SendAsync<ProfileResponse>(HttpMethod.Put, "api/users/profile", request, ct);

    public Task<ImageUploadResponse> UploadProfilePictureAsync(IFormFile file, CancellationToken ct = default) =>
        UploadAsync("api/users/profile/picture", file, ct);

    // Events
    public Task<List<EventSummaryResponse>> GetEventsAsync(string? type, DateOnly? date, string? location, CancellationToken ct = default)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(type)) query.Add($"type={Uri.EscapeDataString(type)}");
        if (date is not null) query.Add($"date={date:yyyy-MM-dd}");
        if (!string.IsNullOrWhiteSpace(location)) query.Add($"location={Uri.EscapeDataString(location.Trim())}");
        var url = query.Count == 0 ? "api/events" : $"api/events?{string.Join('&', query)}";
        return SendAsync<List<EventSummaryResponse>>(HttpMethod.Get, url, null, ct);
    }

    public Task<EventDetailResponse> GetEventAsync(int id, CancellationToken ct = default) =>
        SendAsync<EventDetailResponse>(HttpMethod.Get, $"api/events/{id}", null, ct);

    public Task<List<OrganiserEventResponse>> GetMyEventsAsync(CancellationToken ct = default) =>
        SendAsync<List<OrganiserEventResponse>>(HttpMethod.Get, "api/events/mine", null, ct);

    public Task<EventDetailResponse> CreateEventAsync(EventRequest request, CancellationToken ct = default) =>
        SendAsync<EventDetailResponse>(HttpMethod.Post, "api/events", request, ct);

    public Task<EventDetailResponse> UpdateEventAsync(int id, EventRequest request, CancellationToken ct = default) =>
        SendAsync<EventDetailResponse>(HttpMethod.Put, $"api/events/{id}", request, ct);

    public Task DeleteEventAsync(int id, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Delete, $"api/events/{id}", null, ct);

    public Task<ImageUploadResponse> UploadBannerAsync(int eventId, IFormFile file, CancellationToken ct = default) =>
        UploadAsync($"api/events/{eventId}/upload-banner", file, ct);

    // Categories
    public Task<CategoryResponse> AddCategoryAsync(int eventId, CategoryRequest request, CancellationToken ct = default) =>
        SendAsync<CategoryResponse>(HttpMethod.Post, $"api/events/{eventId}/categories", request, ct);

    public Task<CategoryResponse> UpdateCategoryAsync(int id, CategoryRequest request, CancellationToken ct = default) =>
        SendAsync<CategoryResponse>(HttpMethod.Put, $"api/categories/{id}", request, ct);

    public Task DeleteCategoryAsync(int id, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Delete, $"api/categories/{id}", null, ct);

    // Enrolments
    public Task<List<EnrolmentResponse>> GetMyEnrolmentsAsync(CancellationToken ct = default) =>
        SendAsync<List<EnrolmentResponse>>(HttpMethod.Get, "api/enrolments", null, ct);

    public Task<List<EnrolmentResponse>> GetEventEnrolmentsAsync(int eventId, CancellationToken ct = default) =>
        SendAsync<List<EnrolmentResponse>>(HttpMethod.Get, $"api/events/{eventId}/enrolments", null, ct);

    public Task<EnrolmentResponse> EnrolAsync(int eventId, int categoryId, CancellationToken ct = default) =>
        SendAsync<EnrolmentResponse>(HttpMethod.Post, $"api/events/{eventId}/enrol", new EnrolRequest(categoryId), ct);

    public Task<EnrolmentResponse> SetEnrolmentStatusAsync(int enrolmentId, string status, CancellationToken ct = default) =>
        SendAsync<EnrolmentResponse>(HttpMethod.Put, $"api/enrolments/{enrolmentId}/status", new StatusRequest(status), ct);

    // Results
    public Task<List<ResultResponse>> GetMyResultsAsync(CancellationToken ct = default) =>
        SendAsync<List<ResultResponse>>(HttpMethod.Get, "api/results/me", null, ct);

    public Task<List<ResultResponse>> GetEventResultsAsync(int eventId, CancellationToken ct = default) =>
        SendAsync<List<ResultResponse>>(HttpMethod.Get, $"api/events/{eventId}/results", null, ct);

    public Task<ResultResponse> CaptureResultAsync(int eventId, ResultRequest request, CancellationToken ct = default) =>
        SendAsync<ResultResponse>(HttpMethod.Post, $"api/events/{eventId}/results", request, ct);

    public Task<ResultResponse> UpdateResultAsync(int resultId, UpdateResultRequest request, CancellationToken ct = default) =>
        SendAsync<ResultResponse>(HttpMethod.Put, $"api/results/{resultId}", request, ct);

    private async Task<T> SendAsync<T>(HttpMethod method, string url, object? body, CancellationToken ct)
    {
        using var response = await SendRawAsync(method, url, body, ct);
        return (await response.Content.ReadFromJsonAsync<T>(Json, ct))!;
    }

    private async Task SendAsync(HttpMethod method, string url, object? body, CancellationToken ct)
    {
        using var response = await SendRawAsync(method, url, body, ct);
    }

    private async Task<HttpResponseMessage> SendRawAsync(HttpMethod method, string url, object? body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, url);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: Json);
        }

        var response = await http.SendAsync(request, ct);
        await ThrowIfFailedAsync(response, ct);
        return response;
    }

    private async Task<ImageUploadResponse> UploadAsync(string url, IFormFile file, CancellationToken ct)
    {
        // The file is streamed straight through to the API. The MVC app never stores it or talks to Azure.
        using var form = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream();
        var content = new StreamContent(stream);
        content.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
        form.Add(content, "file", file.FileName);

        using var response = await http.PostAsync(url, form, ct);
        await ThrowIfFailedAsync(response, ct);
        return (await response.Content.ReadFromJsonAsync<ImageUploadResponse>(Json, ct))!;
    }

    private static async Task ThrowIfFailedAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var message = await ReadErrorMessageAsync(response, ct);
        var status = response.StatusCode;
        response.Dispose();
        throw new ApiException(status, message);
    }

    /// <summary>Reads the API's { "message": "..." } body, or a ProblemDetails title, or a general message.</summary>
    private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                if (doc.RootElement.TryGetProperty("message", out var message) && message.GetString() is { Length: > 0 } text)
                {
                    return text;
                }

                if (doc.RootElement.TryGetProperty("title", out var title) && title.GetString() is { Length: > 0 } titleText)
                {
                    return titleText;
                }
            }
        }
        catch (JsonException)
        {
            // Not JSON. Fall through to the general message.
        }

        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Please log in to continue.",
            HttpStatusCode.Forbidden => "You don't have access to that.",
            HttpStatusCode.NotFound => "We couldn't find that.",
            _ => "Something went wrong. Please try again."
        };
    }
}
