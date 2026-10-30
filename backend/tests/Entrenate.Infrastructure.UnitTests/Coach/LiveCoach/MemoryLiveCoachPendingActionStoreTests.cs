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

        private static PendingLiveCoachAction CreateAction(string userId, DateTime expiresAtUtc)
            => new(
                Guid.NewGuid(),
                userId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                1,
                100m,
                8,
                1,
                expiresAtUtc);
    }
}
