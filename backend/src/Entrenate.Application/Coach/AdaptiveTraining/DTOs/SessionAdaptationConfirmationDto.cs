namespace Entrenate.Application.Coach.AdaptiveTraining.DTOs
{
    public sealed record SessionAdaptationConfirmationDto(
        Guid SessionId,
        string ProposalId,
        bool CreatedNow);
}

