using Entrenate.Application.Coach.AdaptiveTraining.DTOs;
using Entrenate.Application.Coach.AdaptiveTraining.Proposal;
using MediatR;

namespace Entrenate.Application.Coach.AdaptiveTraining.Commands.ConfirmSessionAdaptationProposal
{
    public sealed record ConfirmSessionAdaptationProposalCommand(
        string ProposalId,
        string ProposalHash,
        Guid RoutineDayId,
        int? AvailableMinutes,
        IReadOnlyCollection<Guid>? AvailableEquipmentIds,
        IReadOnlyCollection<Guid>? ExcludedExerciseIds,
        SessionEffortPreference EffortPreference)
        : IRequest<SessionAdaptationConfirmationDto>;
}

