using System.Text.Json;

namespace TORSEPAN.Panel.Services;

// Circuit/device scoped via ApiClient. Never persists credentials or private data.
public sealed class LookupResponseCache(TimeProvider? clock = null)
{
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;
    private readonly object _gate = new();
    private readonly Dictionary<string, Entry> _entries = [];
    private string? _identity;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private sealed record Entry(DateTimeOffset Expires, Task<string> Response);

    public async Task<T?> GetAsync<T>(string identity, string url, Func<Task<string>> load)
    {
        Entry entry;
        lock (_gate)
        {
            if (_identity != identity) { _entries.Clear(); _identity = identity; }
            if (!_entries.TryGetValue(url, out entry!) || entry.Expires <= _clock.GetUtcNow())
            {
                if (_entries.Count >= 16) _entries.Clear();
                entry = new(_clock.GetUtcNow().AddSeconds(60), load());
                _entries[url] = entry;
            }
        }
        try
        {
            // Deserialize each time: editing a DTO must not alter later page reads.
            return JsonSerializer.Deserialize<T>(await entry.Response, Json);
        }
        catch
        {
            lock (_gate)
                if (_entries.TryGetValue(url, out var current) && ReferenceEquals(current, entry))
                    _entries.Remove(url);
            throw;
        }
    }

    public void Clear() { lock (_gate) _entries.Clear(); }
}
