using System;
using System.Collections.Concurrent;
using Entrenate.Application.Common.Interfaces;

namespace Entrenate.Infrastructure.Coach.LiveCoach
{
    public sealed class MemoryLiveCoachResponseCache : ILiveCoachResponseCache
    {
        private sealed record CachedEntry(string Text, DateTime ExpiresAtUtc);

        private readonly ConcurrentDictionary<Guid, CachedEntry> _cache = new();

        public void Set(Guid responseId, string text, TimeSpan ttl)
        {
            var expiresAtUtc = DateTime.UtcNow.Add(ttl);
            _cache[responseId] = new CachedEntry(text, expiresAtUtc);
            CleanupExpired();
        }

        public bool TryGet(Guid responseId, out string? text)
        {
            text = null;
            if (_cache.TryGetValue(responseId, out var entry))
            {
                if (DateTime.UtcNow <= entry.ExpiresAtUtc)
                {
                    text = entry.Text;
                    return true;
                }

                _cache.TryRemove(responseId, out _);
            }

            return false;
        }

        private void CleanupExpired()
        {
            var now = DateTime.UtcNow;
            foreach (var kvp in _cache)
            {
                if (now > kvp.Value.ExpiresAtUtc)
                {
                    _cache.TryRemove(kvp.Key, out _);
                }
            }
        }
    }
}

