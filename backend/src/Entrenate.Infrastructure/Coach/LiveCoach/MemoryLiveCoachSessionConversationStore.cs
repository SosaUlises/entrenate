using System.Collections.Concurrent;
using Entrenate.Application.Coach.LiveCoach.Conversation;

namespace Entrenate.Infrastructure.Coach.LiveCoach
{
    public sealed class MemoryLiveCoachSessionConversationStore
        : ILiveCoachSessionConversationStore
    {
        private sealed record Entry(
            LiveCoachSessionConversationContext Context,
            DateTime ExpiresAtUtc);

        private readonly ConcurrentDictionary<(string UserId, Guid SessionId), Entry> _entries = new();
        private readonly TimeSpan _ttl;

        public MemoryLiveCoachSessionConversationStore()
            : this(TimeSpan.FromMinutes(15))
        {
        }

        public MemoryLiveCoachSessionConversationStore(TimeSpan ttl)
        {
            if (ttl <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(ttl));
            _ttl = ttl;
        }

        public LiveCoachSessionConversationContext? Get(
            string userId,
            Guid sessionId,
            DateTime nowUtc)
        {
            var key = (userId, sessionId);
            while (_entries.TryGetValue(key, out var entry))
            {
                if (nowUtc > entry.ExpiresAtUtc)
                {
                    _entries.TryRemove(new KeyValuePair<(string, Guid), Entry>(key, entry));
                    return null;
                }

                var renewedContext = entry.Context with { LastUpdatedAtUtc = nowUtc };
                var renewed = new Entry(renewedContext, nowUtc.Add(_ttl));
                if (_entries.TryUpdate(key, renewed, entry))
                    return renewedContext;
            }

            return null;
        }

        public void Set(LiveCoachSessionConversationContext context, DateTime nowUtc)
        {
            var updated = context with { LastUpdatedAtUtc = nowUtc };
            _entries[(context.UserId, context.SessionId)] = new(updated, nowUtc.Add(_ttl));
        }

        public void Clear(string userId, Guid sessionId)
            => _entries.TryRemove((userId, sessionId), out _);
    }
}
