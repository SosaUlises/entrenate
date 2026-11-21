using Entrenate.Application.Coach.LiveCoach.Adaptation;
using Entrenate.Application.Coach.LiveCoach.Clarifications;
using Entrenate.Application.Coach.LiveCoach.Routing;

namespace Entrenate.Application.Coach.LiveCoach.Conversation
{
    public enum LiveCoachProactiveTriggerType
    {
        RepeatedUnderperformance,
        ExcessiveEffort,
        SessionTimeDrift,
        LongRest
    }

    public sealed record LiveCoachProactiveTriggerReference(
        LiveCoachProactiveTriggerType Type,
        Guid? ExerciseSessionId,
        string SourceFingerprint,
        DateTime TriggeredAtUtc,
        string EvidenceText);

    public sealed record LiveCoachTimeConstraintReference(
        int AvailableMinutes,
        DateTime CapturedAtUtc);

    public sealed record LiveCoachAdaptationReference(
        Guid ProposalId,
        IReadOnlyCollection<Guid> AffectedExerciseSessionIds,
        IReadOnlyCollection<LiveAdaptationReason> Reasons,
        DateTime ProposalExpiresAtUtc);

    public sealed record LiveCoachPrescriptionAdaptationReference(
        Guid ExerciseSessionId,
        string ExerciseName,
        int EffectiveFromSetNumber,
        int PreviousRepMin,
        int PreviousRepMax,
        int PreviousRirMin,
        int PreviousRirMax,
        int NewRepMin,
        int NewRepMax,
        int NewRirMin,
        int NewRirMax,
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
        LiveCoachPrescriptionAdaptationReference? LastPrescriptionAdaptation = null,
        LiveCoachTimeConstraintReference? LastTimeConstraint = null,
        IReadOnlyCollection<LiveCoachProactiveTriggerReference>? RecentProactiveTriggers = null,
        LiveCoachProactiveTriggerReference? LastProactiveTrigger = null,
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
