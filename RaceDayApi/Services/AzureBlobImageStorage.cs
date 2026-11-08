using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;

namespace RaceDayApi.Services;

/// <summary>
/// Saves images to an Azure Blob Storage container and returns the blob's public URL, which the API stores
/// in the database. The MVC app never talks to Azure. It sends the file to the API, and the API sends it here.
/// </summary>
public class AzureBlobImageStorage(BlobContainerClient container, ILogger<AzureBlobImageStorage> logger) : IImageStorage
{
    // The container is created once per process, on the first upload.
    private static readonly SemaphoreSlim CreateLock = new(1, 1);
    private static bool _containerReady;

    public async Task<string> SaveAsync(IFormFile file, string folder, CancellationToken cancellationToken)
    {
        await EnsureContainerAsync(cancellationToken);

        // The folder becomes a prefix in the blob name, for example banners/3f2a....png.
        var blob = container.GetBlobClient($"{folder}/{ImageNames.NewFileName(file.FileName)}");

        await using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream, new BlobUploadOptions
        {
            // Without a content type the browser downloads the image instead of showing it.
            HttpHeaders = new BlobHttpHeaders { ContentType = ImageNames.ContentTypeFor(file.FileName) }
        }, cancellationToken);

        logger.LogInformation("Uploaded {Blob} to Azure Blob Storage", blob.Name);
        return blob.Uri.ToString();
    }

    private async Task EnsureContainerAsync(CancellationToken cancellationToken)
    {
        if (_containerReady)
        {
            return;
        }

        await CreateLock.WaitAsync(cancellationToken);
        try
        {
            if (_containerReady)
            {
                return;
            }

            try
            {
                // Blob-level public read lets the browser load an image from its URL, without listing the container.
                await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);
            }
            catch (RequestFailedException ex) when (ex.ErrorCode == "PublicAccessNotPermitted")
            {
                // New storage accounts block anonymous access by default. The upload still works, but the
                // browser cannot show the images until "Allow Blob anonymous access" is turned on (see README).
                logger.LogWarning("Anonymous blob access is disabled on the storage account, so images will not display. "
                    + "Turn on 'Allow Blob anonymous access' in the Azure portal.");
                await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
            }

            _containerReady = true;
        }
        finally
        {
            CreateLock.Release();
        }
    }
}
