using Entrenate.Application.Coach.LiveCoach.Clarifications;
using Entrenate.Infrastructure.Coach.LiveCoach;
using Xunit;

namespace Entrenate.Infrastructure.UnitTests.Coach.LiveCoach
{
    public sealed class MemoryLiveCoachClarificationStoreTests
    {
        [Fact]
        public void TakeActive_ConsumesClarificationOnlyOnce()
        {
            var store = new MemoryLiveCoachClarificationStore();
            var clarification = CreateClarification(DateTime.UtcNow.AddSeconds(45));
            store.Set(clarification);

            var first = store.TakeActive(clarification.UserId, clarification.SessionId, DateTime.UtcNow);
            var second = store.TakeActive(clarification.UserId, clarification.SessionId, DateTime.UtcNow);

            Assert.Equal(PendingLiveCoachClarificationLookupStatus.Active, first.Status);
            Assert.Equal(PendingLiveCoachClarificationLookupStatus.NotFound, second.Status);
        }

        [Fact]
        public void FindActive_ReportsAndRemovesExpiredClarification()
        {
            var store = new MemoryLiveCoachClarificationStore();
            var clarification = CreateClarification(DateTime.UtcNow.AddSeconds(-1));
            store.Set(clarification);

            var expired = store.FindActive(
                clarification.UserId,
                clarification.SessionId,
                DateTime.UtcNow);
            var removed = store.FindActive(
                clarification.UserId,
                clarification.SessionId,
                DateTime.UtcNow);

            Assert.Equal(PendingLiveCoachClarificationLookupStatus.Expired, expired.Status);
            Assert.Equal(PendingLiveCoachClarificationLookupStatus.NotFound, removed.Status);
        }

        [Fact]
        public void Set_ReplacesExistingClarificationForUserAndSession()
        {
            var store = new MemoryLiveCoachClarificationStore();
            var first = CreateClarification(DateTime.UtcNow.AddSeconds(45));
            var replacement = first with { ClarificationId = Guid.NewGuid() };
            store.Set(first);
            store.Set(replacement);

            var result = store.FindActive(first.UserId, first.SessionId, DateTime.UtcNow);

            Assert.Equal(replacement.ClarificationId, result.Clarification?.ClarificationId);
        }

        private static PendingLiveCoachClarification CreateClarification(DateTime expiresAtUtc)
            => new(
                Guid.NewGuid(),
                "owner",
                Guid.NewGuid(),
                "source-turn",
                Guid.NewGuid(),
                LiveCoachClarificationKind.BestSetCriterion,
                [
                    LiveCoachBestSetCriterion.Weight,
                    LiveCoachBestSetCriterion.Repetitions,
                    LiveCoachBestSetCriterion.Volume
                ],
                expiresAtUtc);
    }
}
