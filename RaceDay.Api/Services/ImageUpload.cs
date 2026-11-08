namespace RaceDay.Api.Services;

public static class ImageUpload
{
    public const long MaxSizeInBytes = 5 * 1024 * 1024;

    public static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png", "image/webp" };

    /// <summary>Returns null when the file can be uploaded, otherwise the reason it cannot.</summary>
    public static string? FindProblem(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return "Choose an image file to upload.";
        }

        if (file.Length > MaxSizeInBytes)
        {
            return "The image must be 5 MB or smaller.";
        }

        if (!AllowedContentTypes.Contains(file.ContentType))
        {
            return "The image must be a JPEG, PNG or WebP file.";
        }

        return null;
    }
}
