namespace Entrenate.Application.Coach.LiveCoach.Clarifications
{
    public enum LiveCoachClarificationKind
    {
        BestSetCriterion
    }

    public enum LiveCoachBestSetCriterion
    {
        Weight,
        Repetitions,
        Volume
    }

    public sealed record PendingLiveCoachClarification(
        Guid ClarificationId,
        string UserId,
        Guid SessionId,
        string SourceClientTurnId,
        Guid ExerciseSessionId,
        LiveCoachClarificationKind Kind,
        IReadOnlyCollection<LiveCoachBestSetCriterion> ValidCriteria,
        DateTime ExpiresAtUtc);

    public enum PendingLiveCoachClarificationLookupStatus
    {
        Active,
        NotFound,
        Expired
    }

    public sealed record PendingLiveCoachClarificationLookupResult(
        PendingLiveCoachClarificationLookupStatus Status,
        PendingLiveCoachClarification? Clarification = null);

    public interface ILiveCoachClarificationStore
    {
        void Set(PendingLiveCoachClarification clarification);

        PendingLiveCoachClarificationLookupResult FindActive(
            string userId,
            Guid sessionId,
            DateTime nowUtc);

        PendingLiveCoachClarificationLookupResult TakeActive(
            string userId,
            Guid sessionId,
            DateTime nowUtc);

        void Clear(string userId, Guid sessionId);
    }
}
