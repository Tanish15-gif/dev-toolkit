using DevToolkit.Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace DevToolkit.Infrastructure.Middleware;

/// <summary>A bounded, process-local store. Entries in use never expire or get evicted.</summary>
public sealed class SubmissionIdempotencyStore : IDisposable
{
    private readonly Dictionary<string, Entry> _entries = new(StringComparer.Ordinal);
    private readonly object _sync = new();
    private readonly BackendSettings _settings;
    private readonly TimeProvider _clock;

    public SubmissionIdempotencyStore(IOptions<BackendSettings> options, TimeProvider clock)
    {
        _settings = options.Value;
        _clock = clock;
    }

    public Entry? Acquire(string key)
    {
        lock (_sync)
        {
            var now = _clock.GetUtcNow();
            var expiredKeys = _entries
                .Where(pair => pair.Value.References == 0 && pair.Value.ExpiresAt <= now)
                .Select(pair => pair.Key)
                .ToArray();

            foreach (var expiredKey in expiredKeys)
            {
                _entries[expiredKey].Gate.Dispose();
                _entries.Remove(expiredKey);
            }

            if (!_entries.TryGetValue(key, out var entry))
            {
                if (_entries.Count >= _settings.IdempotencyMaxEntries)
                {
                    return null;
                }

                entry = new Entry();
                _entries.Add(key, entry);
            }

            entry.References++;

            return entry;
        }
    }

    public void Release(Entry entry)
    {
        lock (_sync)
        {
            entry.References--;
            entry.ExpiresAt = _clock.GetUtcNow().AddSeconds(_settings.IdempotencyRetentionSeconds);
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            foreach (var entry in _entries.Values)
            {
                entry.Gate.Dispose();
            }

            _entries.Clear();
        }
    }

    public sealed class Entry
    {
        internal int References { get; set; }
        internal DateTimeOffset ExpiresAt { get; set; }
        public SemaphoreSlim Gate { get; } = new(1, 1);
        public string? RequestHash { get; set; }
        public byte[]? Response { get; set; }
        public string? ContentType { get; set; }
        public string? Location { get; set; }
        public string? ETag { get; set; }
        public int StatusCode { get; set; }
        public bool CannotReplay { get; set; }
    }
}
