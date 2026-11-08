using Microsoft.AspNetCore.Http;

namespace RaceDayApi.Services;

/// <summary>
/// Stores uploaded images (event banners and profile pictures). Azure Blob Storage is used when a storage
/// connection string is configured; otherwise the images go to the API's own disk.
/// </summary>
public interface IImageStorage
{
    /// <summary>Saves the file in the given folder and returns the URL the browser can load it from.</summary>
    /// <param name="file">The uploaded image. The caller has already checked its size and extension.</param>
    /// <param name="folder">An <see cref="ImageFolders"/> value, such as banners or profile-pictures.</param>
    /// <param name="cancellationToken">Stops the upload if the request is cancelled.</param>
    Task<string> SaveAsync(IFormFile file, string folder, CancellationToken cancellationToken);
}

/// <summary>The folders (or blob name prefixes) the API keeps images in.</summary>
public static class ImageFolders
{
    public const string Banners = "banners";
    public const string ProfilePictures = "profile-pictures";
}
