using System.Net;
using System.Net.Http.Json;
using RaceDayApi.Contracts;
using RaceDayApi.Tests.Infrastructure;

namespace RaceDayApi.Tests;

/// <summary>GET /api/events/mine feeds the organiser dashboard in the MVC app.</summary>
public class OrganiserDashboardTests(RaceDayApiFactory factory) : IClassFixture<RaceDayApiFactory>
{
    [Fact]
    public async Task Organiser_SeesOnlyOwnEvents_WithEnrolmentCounts()
    {
        var organiser = await ApiTestHelpers.OrganiserClientAsync(factory);
        var busyEventId = await ApiTestHelpers.CreateEventAsync(organiser, "Jarred Busy Run");
        var categoryId = await ApiTestHelpers.CreateCategoryAsync(organiser, busyEventId);
        await ApiTestHelpers.CreateEventAsync(organiser, "Jarred Quiet Run", date: new DateTime(2027, 7, 1, 7, 0, 0));

        var otherOrganiser = await ApiTestHelpers.OrganiserClientAsync(factory, "Other Events");
        await ApiTestHelpers.CreateEventAsync(otherOrganiser, "Someone Else's Run");

        var participant = await ApiTestHelpers.ParticipantClientAsync(factory);
        (await participant.PostAsJsonAsync($"/api/events/{busyEventId}/enrol", new EnrolRequest(categoryId))).EnsureSuccessStatusCode();

        var response = await organiser.GetAsync("/api/events/mine");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var events = await ApiTestHelpers.ReadAsync<List<OrganiserEventResponse>>(response);
        Assert.Equal(["Jarred Busy Run", "Jarred Quiet Run"], events!.Select(e => e.Name));
        var busy = events.Single(e => e.EventId == busyEventId);
        Assert.Equal(1, busy.CategoryCount);
        Assert.Equal(1, busy.EnrolmentCount);
        Assert.Equal(0, busy.ResultCount);
        Assert.Equal(0, events.Single(e => e.Name == "Jarred Quiet Run").EnrolmentCount);
    }

    [Fact]
    public async Task Participant_CannotListOrganiserEvents_Returns403()
    {
        var participant = await ApiTestHelpers.ParticipantClientAsync(factory);

        var response = await participant.GetAsync("/api/events/mine");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotListOrganiserEvents_Returns401()
    {
        var response = await factory.CreateClient().GetAsync("/api/events/mine");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
