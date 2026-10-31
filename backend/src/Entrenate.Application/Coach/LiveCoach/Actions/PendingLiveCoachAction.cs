namespace Entrenate.Application.Coach.LiveCoach.Actions
{
    public static class LiveCoachActionTypes
    {
        public const string RegisterCurrentSet = "RegisterCurrentSet";
    }

    public sealed record PendingLiveCoachAction(
        Guid ActionId,
        string UserId,
        Guid SessionId,
        Guid ExerciseSessionId,
        int SetNumber,
        decimal WeightKg,
        int Repetitions,
        int? Rir,
        DateTime ExpiresAtUtc);

    public enum PendingLiveCoachActionTakeStatus
    {
        Success,
        NotFound,
        NotOwned,
        Expired
    }

    public sealed record PendingLiveCoachActionTakeResult(
        PendingLiveCoachActionTakeStatus Status,
        PendingLiveCoachAction? Action = null);

    public enum PendingLiveCoachActionLookupStatus
    {
        Active,
        NotFound,
        Expired
    }

    public sealed record PendingLiveCoachActionLookupResult(
        PendingLiveCoachActionLookupStatus Status,
        PendingLiveCoachAction? Action = null);

    public interface ILiveCoachPendingActionStore
    {
        void Set(PendingLiveCoachAction action);
        PendingLiveCoachActionLookupResult FindActive(
            string userId,
            Guid sessionId,
            DateTime nowUtc);
        PendingLiveCoachActionTakeResult Take(Guid actionId, string userId, DateTime nowUtc);
        PendingLiveCoachActionTakeResult Cancel(Guid actionId, string userId, DateTime nowUtc);
    }
}
