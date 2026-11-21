using Entrenate.Application.Coach.LiveCoach.Adaptation;
using Entrenate.Application.Coach.LiveCoach.Clarifications;
using Entrenate.Application.Coach.LiveCoach.Routing;

namespace Entrenate.Application.Coach.LiveCoach.Conversation
{
    public sealed record LiveCoachAdaptationReference(
        Guid ProposalId,
        IReadOnlyCollection<Guid> AffectedExerciseSessionIds,
        IReadOnlyCollection<LiveAdaptationReason> Reasons,
        DateTime ProposalExpiresAtUtc);

    public sealed record LiveCoachSessionConversationContext(
        string UserId,
        Guid SessionId,
        LiveCoachIntentKind? LastIntentKind = null,
        Guid? LastReferencedExerciseSessionId = null,
        Guid? LastAuthoritativeExerciseSessionId = null,
        LiveCoachIntentKind? LastPerformanceQueryKind = null,
        LiveCoachBestSetCriterion? LastBestSetCriterion = null,
        LiveCoachAdaptationReference? LastAdaptation = null,
        DateTime LastUpdatedAtUtc = default);

    public interface ILiveCoachSessionConversationStore
    {
        LiveCoachSessionConversationContext? Get(
            string userId,
            Guid sessionId,
            DateTime nowUtc);

        void Set(LiveCoachSessionConversationContext context, DateTime nowUtc);

        void Clear(string userId, Guid sessionId);
    }
}
