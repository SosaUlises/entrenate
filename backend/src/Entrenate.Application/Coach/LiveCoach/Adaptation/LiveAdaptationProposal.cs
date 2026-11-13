namespace Entrenate.Application.Coach.LiveCoach.Adaptation
{
    public enum LiveAdaptationChangeType
    {
        OmitExercise
    }

    public sealed record LiveAdaptationChange(
        LiveAdaptationChangeType Type,
        Guid ExerciseSessionId,
        string ExerciseName);

    public interface ILiveAdaptationProposal
    {
        Guid ProposalId { get; }
        string UserId { get; }
        Guid SessionId { get; }
        DateTime CreatedAtUtc { get; }
        DateTime ExpiresAtUtc { get; }
        string PendingWorkFingerprint { get; }
        IReadOnlyCollection<LiveAdaptationChange> Changes { get; }
        string PublicSummary { get; }
        string SuccessMessage { get; }
    }
}
