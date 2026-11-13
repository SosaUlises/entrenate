using System.Collections.Concurrent;
using Entrenate.Application.Coach.LiveCoach.Clarifications;

namespace Entrenate.Infrastructure.Coach.LiveCoach
{
    public sealed class MemoryLiveCoachClarificationStore : ILiveCoachClarificationStore
    {
        private readonly ConcurrentDictionary<Guid, PendingLiveCoachClarification> _clarifications = new();

        public void Set(PendingLiveCoachClarification clarification)
        {
            Clear(clarification.UserId, clarification.SessionId);
            _clarifications[clarification.ClarificationId] = clarification;
            CleanupExpired(DateTime.UtcNow, clarification.ClarificationId);
        }

        public PendingLiveCoachClarificationLookupResult FindActive(
            string userId,
            Guid sessionId,
            DateTime nowUtc)
        {
            var clarification = Find(userId, sessionId);
            if (clarification is null)
                return new(PendingLiveCoachClarificationLookupStatus.NotFound);

            if (nowUtc > clarification.ExpiresAtUtc)
            {
                _clarifications.TryRemove(clarification.ClarificationId, out _);
                return new(PendingLiveCoachClarificationLookupStatus.Expired);
            }

            return new(PendingLiveCoachClarificationLookupStatus.Active, clarification);
        }

        public PendingLiveCoachClarificationLookupResult TakeActive(
            string userId,
            Guid sessionId,
            DateTime nowUtc)
        {
            var lookup = FindActive(userId, sessionId, nowUtc);
            if (lookup.Status != PendingLiveCoachClarificationLookupStatus.Active ||
                lookup.Clarification is null)
            {
                return lookup;
            }

            return _clarifications.TryRemove(lookup.Clarification.ClarificationId, out var removed)
                ? new(PendingLiveCoachClarificationLookupStatus.Active, removed)
                : new(PendingLiveCoachClarificationLookupStatus.NotFound);
        }

        public void Clear(string userId, Guid sessionId)
        {
            foreach (var entry in _clarifications)
            {
                if (entry.Value.UserId == userId && entry.Value.SessionId == sessionId)
                    _clarifications.TryRemove(entry.Key, out _);
            }
        }

        private PendingLiveCoachClarification? Find(string userId, Guid sessionId)
        {
            return _clarifications.Values
                .Where(item => item.UserId == userId && item.SessionId == sessionId)
                .OrderByDescending(item => item.ExpiresAtUtc)
                .FirstOrDefault();
        }

        private void CleanupExpired(DateTime nowUtc, Guid exceptClarificationId)
        {
            foreach (var entry in _clarifications)
            {
                if (entry.Key != exceptClarificationId && nowUtc > entry.Value.ExpiresAtUtc)
                    _clarifications.TryRemove(entry.Key, out _);
            }
        }
    }
}
