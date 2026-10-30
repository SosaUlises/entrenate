using System.Collections.Concurrent;
using Entrenate.Application.Coach.LiveCoach.Actions;

namespace Entrenate.Infrastructure.Coach.LiveCoach
{
    public sealed class MemoryLiveCoachPendingActionStore : ILiveCoachPendingActionStore
    {
        private readonly ConcurrentDictionary<Guid, PendingLiveCoachAction> _actions = new();

        public void Set(PendingLiveCoachAction action)
        {
            _actions[action.ActionId] = action;
            CleanupExpired(DateTime.UtcNow, action.ActionId);
        }

        public PendingLiveCoachActionTakeResult Take(Guid actionId, string userId, DateTime nowUtc)
            => Remove(actionId, userId, nowUtc);

        public PendingLiveCoachActionTakeResult Cancel(Guid actionId, string userId, DateTime nowUtc)
            => Remove(actionId, userId, nowUtc);

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
