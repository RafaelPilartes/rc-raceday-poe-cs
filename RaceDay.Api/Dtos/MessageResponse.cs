namespace RaceDay.Api.Dtos;

/// <summary>A single human-readable message. Used for every error body the API returns.</summary>
/// <param name="Message">What went wrong, or what the caller should do instead.</param>
public record MessageResponse(string Message);
