using System.Collections.Concurrent;
using Entrenate.Application.Coach.LiveCoach.Actions;

namespace Entrenate.Infrastructure.Coach.LiveCoach
{
    public sealed class MemoryLiveCoachPendingActionStore : ILiveCoachPendingActionStore
    {
        private readonly ConcurrentDictionary<Guid, PendingLiveCoachAction> _actions = new();

        public void Set(PendingLiveCoachAction action)
        {
            foreach (var existing in _actions)
            {
                if (existing.Value.UserId == action.UserId &&
                    existing.Value.SessionId == action.SessionId)
                {
                    _actions.TryRemove(existing.Key, out _);
                }
            }

            _actions[action.ActionId] = action;
            CleanupExpired(DateTime.UtcNow, action.ActionId);
        }

        public PendingLiveCoachActionLookupResult FindActive(
            string userId,
            Guid sessionId,
            DateTime nowUtc)
        {
            var action = _actions.Values
                .Where(item => item.UserId == userId && item.SessionId == sessionId)
                .OrderByDescending(item => item.ExpiresAtUtc)
                .FirstOrDefault();

            if (action is null)
                return new(PendingLiveCoachActionLookupStatus.NotFound);

            if (nowUtc > action.ExpiresAtUtc)
            {
                _actions.TryRemove(action.ActionId, out _);
                return new(PendingLiveCoachActionLookupStatus.Expired);
            }

            return new(PendingLiveCoachActionLookupStatus.Active, action);
        }

        public PendingLiveCoachActionTakeResult Take(Guid actionId, string userId, DateTime nowUtc)
            => Remove(actionId, userId, nowUtc);

        public PendingLiveCoachActionTakeResult Cancel(Guid actionId, string userId, DateTime nowUtc)
            => Remove(actionId, userId, nowUtc);

        public PendingLiveCoachActionUpdateResult Update(
            Guid actionId,
            string userId,
            PendingLiveCoachSetPatch patch,
            DateTime nowUtc)
        {
            while (true)
            {
                if (!_actions.TryGetValue(actionId, out var current))
                    return new(PendingLiveCoachActionUpdateStatus.NotFound);

                if (!string.Equals(current.UserId, userId, StringComparison.Ordinal))
                    return new(PendingLiveCoachActionUpdateStatus.NotOwned);

                if (nowUtc > current.ExpiresAtUtc)
                {
                    _actions.TryRemove(new KeyValuePair<Guid, PendingLiveCoachAction>(actionId, current));
                    return new(PendingLiveCoachActionUpdateStatus.Expired);
                }

                if (current.Type != LiveCoachActionTypes.RegisterCurrentSet)
                    return new(PendingLiveCoachActionUpdateStatus.UnsupportedType);

                var updated = current with
                {
                    WeightKg = patch.WeightKg ?? current.WeightKg,
                    Repetitions = patch.Repetitions ?? current.Repetitions,
                    Rir = patch.Rir ?? current.Rir
                };

                if (_actions.TryUpdate(actionId, updated, current))
                    return new(PendingLiveCoachActionUpdateStatus.Success, updated);
            }
        }

        private PendingLiveCoachActionTakeResult Remove(Guid actionId, string userId, DateTime nowUtc)
        {
            if (!_actions.TryGetValue(actionId, out var action))
                return new(PendingLiveCoachActionTakeStatus.NotFound);

            if (!string.Equals(action.UserId, userId, StringComparison.Ordinal))
                return new(PendingLiveCoachActionTakeStatus.NotOwned);

            if (nowUtc > action.ExpiresAtUtc)
            {
                _actions.TryRemove(actionId, out _);
                return new(PendingLiveCoachActionTakeStatus.Expired);
            }

            return _actions.TryRemove(actionId, out var removed)
                ? new(PendingLiveCoachActionTakeStatus.Success, removed)
                : new(PendingLiveCoachActionTakeStatus.NotFound);
        }

        private void CleanupExpired(DateTime nowUtc, Guid exceptActionId)
        {
            foreach (var entry in _actions)
                if (entry.Key != exceptActionId && nowUtc > entry.Value.ExpiresAtUtc)
                    _actions.TryRemove(entry.Key, out _);
        }
    }
}
