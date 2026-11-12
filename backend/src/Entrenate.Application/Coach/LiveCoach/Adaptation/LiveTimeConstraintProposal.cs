namespace Entrenate.Application.Coach.LiveCoach.Adaptation
{
    public enum LiveTimeConstraintResultStatus
    {
        NoPendingWork,
        FitsAvailableTime,
        CannotAdapt,
        Proposal
    }

    public enum LiveTimeConstraintChangeType
    {
        OmitExercise
    }

    public sealed record LiveTimeConstraintChange(
        LiveTimeConstraintChangeType Type,
        Guid ExerciseSessionId,
        string ExerciseName);

    public sealed record LiveTimeConstraintProposal(
        Guid ProposalId,
        string UserId,
        Guid SessionId,
        DateTime CreatedAtUtc,
        DateTime ExpiresAtUtc,
        int RequestedRemainingMinutes,
        int EstimatedMinutesBefore,
        int EstimatedMinutesAfter,
        string PendingWorkFingerprint,
        IReadOnlyCollection<LiveTimeConstraintChange> Changes,
        string PublicSummary = "");

    public sealed record LiveTimeConstraintResult(
        LiveTimeConstraintResultStatus Status,
        int RequestedRemainingMinutes,
        int EstimatedMinutesBefore,
        int EstimatedMinutesAfter,
        IReadOnlyCollection<LiveTimeConstraintChange> Changes);
}
