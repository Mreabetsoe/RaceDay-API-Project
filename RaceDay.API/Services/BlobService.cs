using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.API.Services;

public class BlobService
{
    private readonly BlobContainerClient _container;

    public BlobService(IConfiguration config)
    {
        var connection = config.GetConnectionString("AzureStorage")
            ?? throw new InvalidOperationException("The AzureStorage connection string is missing.");
        var containerName = config["BlobContainerName"] ?? "raceday-images";
        _container = new BlobContainerClient(connection, containerName);
    }

    public async Task<string> UploadAsync(IFormFile file, string folder)
    {
        await _container.CreateIfNotExistsAsync(PublicAccessType.Blob);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var blob = _container.GetBlobClient($"{folder}/{Guid.NewGuid()}{extension}");

        using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });

        return blob.Uri.ToString();
    }
}