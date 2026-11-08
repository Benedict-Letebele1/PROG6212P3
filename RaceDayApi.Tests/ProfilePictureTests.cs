using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using RaceDayApi.Contracts;
using RaceDayApi.Services;
using RaceDayApi.Tests.Infrastructure;

namespace RaceDayApi.Tests;

/// <summary>Participants upload a profile picture through the API, which picks the image storage.</summary>
public class ProfilePictureTests(RaceDayApiFactory factory) : IClassFixture<RaceDayApiFactory>
{
    // The 8-byte PNG signature is enough for the API, which checks the extension and size.
    private static readonly byte[] PngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private static MultipartFormDataContent ImageForm(string fileName, byte[] bytes, string contentType)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, "file", fileName);
        return form;
    }

    [Fact]
    public async Task Participant_UploadsAPicture_AndTheProfileShowsItsUrl()
    {
        var participant = await ApiTestHelpers.ParticipantClientAsync(factory);
        using var form = ImageForm("jarred.png", PngBytes, "image/png");

        var response = await participant.PostAsync("/api/users/profile/picture", form);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var uploaded = await ApiTestHelpers.ReadAsync<ProfilePictureResponse>(response);
        Assert.StartsWith("/uploads/profile-pictures/", uploaded!.ImageUrl);
        Assert.EndsWith(".png", uploaded.ImageUrl);

        var profile = await ApiTestHelpers.ReadAsync<ProfileResponse>(await participant.GetAsync("/api/users/profile"));
        Assert.Equal(uploaded.ImageUrl, profile!.ProfilePictureUrl);
    }

    [Fact]
    public async Task Participant_UploadsATextFile_Returns400()
    {
        var participant = await ApiTestHelpers.ParticipantClientAsync(factory);
        using var form = ImageForm("notes.txt", [0x54, 0x65, 0x78, 0x74], "text/plain");

        var response = await participant.PostAsync("/api/users/profile/picture", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotUploadAProfilePicture_Returns403()
    {
        var organiser = await ApiTestHelpers.OrganiserClientAsync(factory);
        using var form = ImageForm("jarred.png", PngBytes, "image/png");

        var response = await organiser.PostAsync("/api/users/profile/picture", form);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotUploadAProfilePicture_Returns401()
    {
        using var form = ImageForm("jarred.png", PngBytes, "image/png");

        var response = await factory.CreateClient().PostAsync("/api/users/profile/picture", form);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void Storage_IsLocalDisk_WhenNoAzureConnectionStringIsSet()
    {
        using var scope = factory.Services.CreateScope();

        Assert.IsType<LocalImageStorage>(scope.ServiceProvider.GetRequiredService<IImageStorage>());
    }

    [Fact]
    public void Storage_IsAzureBlob_WhenAConnectionStringIsSet()
    {
        // The Azurite connection string is only parsed here. No request is sent to Azure.
        using var azureFactory = factory.WithWebHostBuilder(builder =>
            builder.UseSetting("AzureStorage:ConnectionString", "UseDevelopmentStorage=true"));
        using var scope = azureFactory.Services.CreateScope();

        Assert.IsType<AzureBlobImageStorage>(scope.ServiceProvider.GetRequiredService<IImageStorage>());
    }
}
