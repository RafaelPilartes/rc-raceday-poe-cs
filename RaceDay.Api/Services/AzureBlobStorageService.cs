using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services;

public class AzureBlobStorageService : IBlobStorageService
{
    private readonly string? connectionString;
    private readonly string containerName;

    public AzureBlobStorageService(IConfiguration configuration)
    {
        connectionString = configuration["AzureStorage:ConnectionString"];
        containerName = configuration["AzureStorage:ContainerName"] ?? "raceday-images";
    }

    public async Task<string> UploadImageAsync(Stream content, string fileName, string contentType)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "AzureStorage:ConnectionString is not set. Add it with dotnet user-secrets.");
        }

        var container = new BlobContainerClient(connectionString, containerName);

        // Blob-level public access so the MVC app can put the URL straight into an img tag.
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob);

        var blobName = $"{Guid.NewGuid():N}{Path.GetExtension(fileName)}";
        var blob = container.GetBlobClient(blobName);
        await blob.UploadAsync(content, new BlobHttpHeaders { ContentType = contentType });

        return blob.Uri.ToString();
    }
}
