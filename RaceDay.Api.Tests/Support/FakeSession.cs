using Microsoft.AspNetCore.Http;

namespace RaceDay.Api.Tests.Support;

/// <summary>
/// A session backed by a dictionary. The real one needs a running host and a
/// cookie round trip, which unit tests have neither of, but the controllers and
/// the SessionAuthorize filter only ever talk to ISession.
/// </summary>
internal sealed class FakeSession : ISession
{
    private readonly Dictionary<string, byte[]> values = new();

    public bool IsAvailable => true;

    public string Id { get; } = Guid.NewGuid().ToString();

    public IEnumerable<string> Keys => values.Keys;

    public void Clear()
    {
        values.Clear();
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task LoadAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public void Remove(string key)
    {
        values.Remove(key);
    }

    public void Set(string key, byte[] value)
    {
        values[key] = value;
    }

    public bool TryGetValue(string key, out byte[] value)
    {
        return values.TryGetValue(key, out value!);
    }
}
