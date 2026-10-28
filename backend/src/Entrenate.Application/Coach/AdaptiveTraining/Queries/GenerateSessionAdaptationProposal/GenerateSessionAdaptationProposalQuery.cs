using Entrenate.Application.Coach.AdaptiveTraining.DTOs;
using Entrenate.Application.Coach.AdaptiveTraining.Proposal;
using MediatR;

namespace Entrenate.Application.Coach.AdaptiveTraining.Queries.GenerateSessionAdaptationProposal
{
    public sealed record GenerateSessionAdaptationProposalQuery(
        Guid RoutineDayId,
        int? AvailableMinutes,
        IReadOnlyCollection<Guid>? AvailableEquipmentIds,
        IReadOnlyCollection<Guid>? ExcludedExerciseIds,
        SessionEffortPreference EffortPreference)
        : IRequest<SessionAdaptationProposalDto>;
}

