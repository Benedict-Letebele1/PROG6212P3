using Microsoft.AspNetCore.Http;

namespace RaceDayApi.Services;

/// <summary>
/// Keeps images in wwwroot/uploads/{folder}. This is the fallback when no Azure storage is configured,
/// so the API and the tests still run on a machine without a cloud account.
/// </summary>
public class LocalImageStorage(IWebHostEnvironment environment) : IImageStorage
{
    public async Task<string> SaveAsync(IFormFile file, string folder, CancellationToken cancellationToken)
    {
        // Program.cs serves wwwroot/uploads at /uploads.
        var target = Path.Combine(environment.ContentRootPath, "wwwroot", "uploads", folder);

        // The folder is created on first use, so a fresh clone runs without it.
        Directory.CreateDirectory(target);

        var fileName = ImageNames.NewFileName(file.FileName);

        await using var stream = File.Create(Path.Combine(target, fileName));
        await file.CopyToAsync(stream, cancellationToken);

        return $"/uploads/{folder}/{fileName}";
    }
}
