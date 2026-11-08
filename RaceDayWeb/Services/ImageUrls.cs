using Microsoft.Extensions.Options;

namespace RaceDayWeb.Services;

/// <summary>
/// Turns an image URL from the API into one the browser can load. Azure Blob URLs are absolute and are used as
/// they are. Images on the API's own disk come back as /uploads/... and get the API's public address in front.
/// </summary>
public class ImageUrls(IOptions<ApiOptions> options)
{
    public string? Resolve(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

        var baseUrl = (options.Value.PublicUrl ?? options.Value.BaseUrl).TrimEnd('/');
        return $"{baseUrl}/{url.TrimStart('/')}";
    }
}
