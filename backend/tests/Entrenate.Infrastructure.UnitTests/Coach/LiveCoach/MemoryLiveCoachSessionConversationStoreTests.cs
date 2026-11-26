using Entrenate.Application.Coach.LiveCoach.Conversation;
using Entrenate.Application.Coach.LiveCoach.Routing;
using Entrenate.Infrastructure.Coach.LiveCoach;
using Xunit;

namespace Entrenate.Infrastructure.UnitTests.Coach.LiveCoach
{
    public sealed class MemoryLiveCoachSessionConversationStoreTests
    {
        [Fact]
        public void Store_IsScopedByUserAndSession()
        {
            var store = new MemoryLiveCoachSessionConversationStore(TimeSpan.FromMinutes(15));
            var now = DateTime.UtcNow;
            var firstSession = Guid.NewGuid();
            var secondSession = Guid.NewGuid();
            store.Set(new("user-a", firstSession, LiveCoachIntentKind.RemainingSets), now);

            Assert.NotNull(store.Get("user-a", firstSession, now));
            Assert.Null(store.Get("user-b", firstSession, now));
            Assert.Null(store.Get("user-a", secondSession, now));
        }

        [Fact]
        public void Store_ExpiresAndRenewsContextUsingSlidingTtl()
        {
            var store = new MemoryLiveCoachSessionConversationStore(TimeSpan.FromMinutes(10));
            var sessionId = Guid.NewGuid();
            var createdAt = DateTime.UtcNow;
            store.Set(new("user", sessionId, LiveCoachIntentKind.RemainingSets), createdAt);

            var renewed = store.Get("user", sessionId, createdAt.AddMinutes(9));
            var stillActive = store.Get("user", sessionId, createdAt.AddMinutes(18));
            var expired = store.Get("user", sessionId, createdAt.AddMinutes(29));

            Assert.NotNull(renewed);
            Assert.NotNull(stillActive);
            Assert.Null(expired);
        }

        [Fact]
        public void Clear_RemovesOnlyRequestedSessionContext()
        {
            var store = new MemoryLiveCoachSessionConversationStore();
            var now = DateTime.UtcNow;
            var firstSession = Guid.NewGuid();
            var secondSession = Guid.NewGuid();
            store.Set(new("user", firstSession), now);
            store.Set(new("user", secondSession), now);

            store.Clear("user", firstSession);

            Assert.Null(store.Get("user", firstSession, now));
            Assert.NotNull(store.Get("user", secondSession, now));
        }
    }
}
