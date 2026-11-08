namespace RaceDayApi.Services;

/// <summary>Shared rules for the images the API accepts.</summary>
public static class ImageNames
{
    public static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    /// <summary>A random file name that keeps the original extension, so uploads never overwrite each other.</summary>
    public static string NewFileName(string originalFileName) =>
        $"{Guid.NewGuid():N}{Path.GetExtension(originalFileName).ToLowerInvariant()}";

    /// <summary>The content type to store with the image, so browsers display it instead of downloading it.</summary>
    public static string ContentTypeFor(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".webp" => "image/webp",
        _ => "image/jpeg"
    };

    /// <summary>Returns an error message when the file is empty, too big or not an allowed image type.</summary>
    public static string? UploadError(IFormFile? file, long maxBytes, string label)
    {
        if (file is null || file.Length == 0)
        {
            return "Choose an image to upload.";
        }

        if (file.Length > maxBytes)
        {
            return $"{label} must be {maxBytes / (1024 * 1024)} MB or smaller.";
        }

        return AllowedExtensions.Contains(Path.GetExtension(file.FileName).ToLowerInvariant())
            ? null
            : $"{label} must be a JPG, PNG or WebP image.";
    }
}
