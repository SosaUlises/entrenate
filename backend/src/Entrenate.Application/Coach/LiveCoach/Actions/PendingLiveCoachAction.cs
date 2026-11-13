using Entrenate.Application.Coach.LiveCoach.Adaptation;

namespace Entrenate.Application.Coach.LiveCoach.Actions
{
    public static class LiveCoachActionTypes
    {
        public const string RegisterCurrentSet = "RegisterCurrentSet";
        public const string SkipCurrentExercise = "SkipCurrentExercise";
        public const string ReactivateExercise = "ReactivateExercise";
        public const string ApplyTimeConstraintAdaptation = "ApplyTimeConstraintAdaptation";
        public const string ApplyFatigueAdaptation = "ApplyFatigueAdaptation";
        public const string ApplyPerformanceAdaptation = "ApplyPerformanceAdaptation";
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
        DateTime ExpiresAtUtc,
        string Type = LiveCoachActionTypes.RegisterCurrentSet,
        string? ExerciseName = null,
        int PendingSetCount = 0,
        ILiveAdaptationProposal? AdaptationProposal = null);

    public sealed record PendingLiveCoachSetPatch(
        decimal? WeightKg = null,
        int? Repetitions = null,
        int? Rir = null)
    {
        public bool HasChanges => WeightKg.HasValue || Repetitions.HasValue || Rir.HasValue;
    }

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

    public enum PendingLiveCoachActionUpdateStatus
    {
        Success,
        NotFound,
        NotOwned,
        Expired,
        UnsupportedType
    }

    public sealed record PendingLiveCoachActionUpdateResult(
        PendingLiveCoachActionUpdateStatus Status,
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
        PendingLiveCoachActionUpdateResult Update(
            Guid actionId,
            string userId,
            PendingLiveCoachSetPatch patch,
            DateTime nowUtc);
    }
}
