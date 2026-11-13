namespace Entrenate.Application.Coach.LiveCoach.Adaptation
{
    public enum LiveTimeConstraintResultStatus
    {
        NoPendingWork,
        FitsAvailableTime,
        CannotAdapt,
        Proposal
    }

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
        IReadOnlyCollection<LiveAdaptationChange> Changes,
        string PublicSummary = "") : ILiveAdaptationProposal
    {
        public string SuccessMessage =>
            $"Listo. Ajusté la sesión para acercarla a {RequestedRemainingMinutes} minutos.";
    }

    public sealed record LiveTimeConstraintResult(
        LiveTimeConstraintResultStatus Status,
        int RequestedRemainingMinutes,
        int EstimatedMinutesBefore,
        int EstimatedMinutesAfter,
        IReadOnlyCollection<LiveAdaptationChange> Changes);
}
