namespace RaceDay.Api.Services;

public interface IBlobStorageService
{
    /// <summary>Stores the image and returns the public URL it can be displayed from.</summary>
    Task<string> UploadImageAsync(Stream content, string fileName, string contentType);
}
