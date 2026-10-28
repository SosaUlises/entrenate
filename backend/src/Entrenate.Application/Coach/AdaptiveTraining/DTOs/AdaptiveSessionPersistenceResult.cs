namespace Entrenate.Application.Coach.AdaptiveTraining.DTOs
{
    public sealed record AdaptiveSessionPersistenceResult(
        Guid SessionId,
        string ProposalId,
        bool CreatedNow);
}

