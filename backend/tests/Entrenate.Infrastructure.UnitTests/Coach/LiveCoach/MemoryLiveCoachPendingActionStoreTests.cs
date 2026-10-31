using Entrenate.Application.Coach.LiveCoach.Actions;
using Entrenate.Infrastructure.Coach.LiveCoach;
using Xunit;

namespace Entrenate.Infrastructure.UnitTests.Coach.LiveCoach
{
    public sealed class MemoryLiveCoachPendingActionStoreTests
    {
        [Fact]
        public void Take_CanConsumeAnActionOnlyOnce()
        {
            var store = new MemoryLiveCoachPendingActionStore();
            var action = CreateAction("owner", DateTime.UtcNow.AddSeconds(45));
            store.Set(action);

            var first = store.Take(action.ActionId, "owner", DateTime.UtcNow);
            var second = store.Take(action.ActionId, "owner", DateTime.UtcNow);

            Assert.Equal(PendingLiveCoachActionTakeStatus.Success, first.Status);
            Assert.Equal(PendingLiveCoachActionTakeStatus.NotFound, second.Status);
        }

        [Fact]
        public void Take_DoesNotExposeOrRemoveAnActionOwnedByAnotherUser()
        {
            var store = new MemoryLiveCoachPendingActionStore();
            var action = CreateAction("owner", DateTime.UtcNow.AddSeconds(45));
            store.Set(action);

            var foreign = store.Take(action.ActionId, "other", DateTime.UtcNow);
            var owner = store.Take(action.ActionId, "owner", DateTime.UtcNow);

            Assert.Equal(PendingLiveCoachActionTakeStatus.NotOwned, foreign.Status);
            Assert.Equal(PendingLiveCoachActionTakeStatus.Success, owner.Status);
        }

        [Fact]
        public void Take_RejectsExpiredAction()
        {
            var store = new MemoryLiveCoachPendingActionStore();
            var action = CreateAction("owner", DateTime.UtcNow.AddSeconds(-1));
            store.Set(action);

            var result = store.Take(action.ActionId, "owner", DateTime.UtcNow);

            Assert.Equal(PendingLiveCoachActionTakeStatus.Expired, result.Status);
        }

        [Fact]
        public void Cancel_RemovesActionWithoutExecutingAnything()
        {
            var store = new MemoryLiveCoachPendingActionStore();
            var action = CreateAction("owner", DateTime.UtcNow.AddSeconds(45));
            store.Set(action);

            var canceled = store.Cancel(action.ActionId, "owner", DateTime.UtcNow);
            var laterConfirm = store.Take(action.ActionId, "owner", DateTime.UtcNow);

            Assert.Equal(PendingLiveCoachActionTakeStatus.Success, canceled.Status);
            Assert.Equal(PendingLiveCoachActionTakeStatus.NotFound, laterConfirm.Status);
        }

        [Fact]
        public void FindActive_UsesUserAndSessionAndRejectsExpiredActions()
        {
            var store = new MemoryLiveCoachPendingActionStore();
            var sessionId = Guid.NewGuid();
            var active = CreateAction("owner", sessionId, DateTime.UtcNow.AddSeconds(45));
            var expired = CreateAction("owner", Guid.NewGuid(), DateTime.UtcNow.AddSeconds(-1));
            store.Set(active);
            store.Set(expired);

            var found = store.FindActive("owner", sessionId, DateTime.UtcNow);
            var foreign = store.FindActive("other", sessionId, DateTime.UtcNow);
            var stale = store.FindActive("owner", expired.SessionId, DateTime.UtcNow);

            Assert.Equal(PendingLiveCoachActionLookupStatus.Active, found.Status);
            Assert.Equal(active.ActionId, found.Action?.ActionId);
            Assert.Equal(PendingLiveCoachActionLookupStatus.NotFound, foreign.Status);
            Assert.Equal(PendingLiveCoachActionLookupStatus.Expired, stale.Status);
        }

        [Fact]
        public void Set_ReplacesThePreviousPendingActionForTheSameUserAndSession()
        {
            var store = new MemoryLiveCoachPendingActionStore();
            var sessionId = Guid.NewGuid();
            var first = CreateAction("owner", sessionId, DateTime.UtcNow.AddSeconds(45));
            var replacement = CreateAction("owner", sessionId, DateTime.UtcNow.AddSeconds(45));
            store.Set(first);
            store.Set(replacement);

            var lookup = store.FindActive("owner", sessionId, DateTime.UtcNow);
            var oldAction = store.Take(first.ActionId, "owner", DateTime.UtcNow);

            Assert.Equal(replacement.ActionId, lookup.Action?.ActionId);
            Assert.Equal(PendingLiveCoachActionTakeStatus.NotFound, oldAction.Status);
        }

        private static PendingLiveCoachAction CreateAction(string userId, DateTime expiresAtUtc)
            => CreateAction(userId, Guid.NewGuid(), expiresAtUtc);

        private static PendingLiveCoachAction CreateAction(
            string userId,
            Guid sessionId,
            DateTime expiresAtUtc)
            => new(
                Guid.NewGuid(),
                userId,
                sessionId,
                Guid.NewGuid(),
                1,
                100m,
                8,
                1,
                expiresAtUtc);
    }
}
