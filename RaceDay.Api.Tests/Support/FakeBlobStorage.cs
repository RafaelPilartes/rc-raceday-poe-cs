using RaceDay.Api.Services;

namespace RaceDay.Api.Tests.Support;

/// <summary>
/// Stands in for Azure Blob Storage so the upload endpoints can be tested
/// without credentials, which is what lets the tests run in CI.
/// </summary>
internal sealed class FakeBlobStorage : IBlobStorageService
{
    public List<string> UploadedFileNames { get; } = new();

    public Task<string> UploadImageAsync(Stream content, string fileName, string contentType)
    {
        UploadedFileNames.Add(fileName);
        return Task.FromResult($"https://fake.blob.core.windows.net/raceday-images/{fileName}");
    }
}
