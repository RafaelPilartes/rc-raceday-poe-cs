using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDay.Web.Security;

namespace RaceDay.Web.Services;

/// <summary>
/// Every call the MVC app makes to the RaceDay API goes through here. The API
/// authenticates with a session cookie, but the MVC app calls it from the
/// server, so that cookie never reaches the browser: it is kept in this app's
/// own session and attached to each outgoing request.
/// </summary>
public class ApiClient
{
    private const string ApiSessionCookieName = "RaceDay.Session";

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly HttpClient httpClient;
    private readonly IHttpContextAccessor httpContextAccessor;

    public ApiClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
    {
        this.httpClient = httpClient;
        this.httpContextAccessor = httpContextAccessor;
    }

    public Task<ApiResult<T>> GetAsync<T>(string path)
    {
        return SendAsync<T>(new HttpRequestMessage(HttpMethod.Get, path));
    }

    public Task<ApiResult<T>> PostAsync<T>(string path, object body)
    {
        return SendAsync<T>(new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body)
        });
    }

    public Task<ApiResult<T>> PutAsync<T>(string path, object body)
    {
        return SendAsync<T>(new HttpRequestMessage(HttpMethod.Put, path)
        {
            Content = JsonContent.Create(body)
        });
    }

    public async Task<ApiResult> PostEmptyAsync(string path)
    {
        return await SendAsync<object>(new HttpRequestMessage(HttpMethod.Post, path));
    }

    public async Task<ApiResult> DeleteAsync(string path)
    {
        return await SendAsync<object>(new HttpRequestMessage(HttpMethod.Delete, path));
    }

    public async Task<ApiResult<T>> PostFileAsync<T>(string path, string fieldName, IFormFile file)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(file.OpenReadStream());
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType);
        content.Add(fileContent, fieldName, file.FileName);

        return await SendAsync<T>(new HttpRequestMessage(HttpMethod.Post, path) { Content = content });
    }

    private async Task<ApiResult<T>> SendAsync<T>(HttpRequestMessage request)
    {
        AttachApiCookie(request);

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request);
        }
        catch (HttpRequestException)
        {
            return new ApiResult<T>
            {
                Succeeded = false,
                StatusCode = HttpStatusCode.ServiceUnavailable,
                ErrorMessage = "The RaceDay API is not responding. Start it and try again."
            };
        }

        RememberApiCookie(response);

        if (!response.IsSuccessStatusCode)
        {
            return new ApiResult<T>
            {
                Succeeded = false,
                StatusCode = response.StatusCode,
                ErrorMessage = await ReadErrorMessageAsync(response)
            };
        }

        return new ApiResult<T>
        {
            Succeeded = true,
            StatusCode = response.StatusCode,
            Value = await ReadValueAsync<T>(response)
        };
    }

    private void AttachApiCookie(HttpRequestMessage request)
    {
        var apiCookie = httpContextAccessor.HttpContext?.Session.GetString(SessionKeys.ApiCookie);
        if (!string.IsNullOrEmpty(apiCookie))
        {
            request.Headers.Add("Cookie", apiCookie);
        }
    }

    private void RememberApiCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders))
        {
            return;
        }

        var sessionCookie = setCookieHeaders
            .FirstOrDefault(header => header.StartsWith(ApiSessionCookieName + "="));

        if (sessionCookie is not null)
        {
            httpContextAccessor.HttpContext?.Session.SetString(
                SessionKeys.ApiCookie, sessionCookie.Split(';')[0]);
        }
    }

    private static async Task<T?> ReadValueAsync<T>(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return default;
        }

        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(body))
        {
            return "The RaceDay API rejected the request.";
        }

        try
        {
            using var document = JsonDocument.Parse(body);

            if (document.RootElement.TryGetProperty("message", out var message))
            {
                return message.GetString() ?? "The RaceDay API rejected the request.";
            }

            // Model validation answers with a ProblemDetails errors object.
            if (document.RootElement.TryGetProperty("errors", out var errors))
            {
                var firstMessage = errors.EnumerateObject()
                    .SelectMany(field => field.Value.EnumerateArray())
                    .Select(entry => entry.GetString())
                    .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));

                if (firstMessage is not null)
                {
                    return firstMessage;
                }
            }
        }
        catch (JsonException)
        {
            // Not JSON, so fall through to the generic message.
        }

        return "The RaceDay API rejected the request.";
    }
}
