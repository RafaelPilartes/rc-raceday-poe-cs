using System.Net;

namespace RaceDay.Web.Services;

public class ApiResult
{
    public bool Succeeded { get; init; }

    public HttpStatusCode StatusCode { get; init; }

    public string? ErrorMessage { get; init; }
}

public class ApiResult<T> : ApiResult
{
    public T? Value { get; init; }
}
