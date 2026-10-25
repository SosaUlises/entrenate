using Entrenate.Application.Coach.RoutinePlanner.DTOs;
using MediatR;

namespace Entrenate.Application.Coach.RoutinePlanner.Commands.ConfirmRoutineProposal
{
    public sealed record ConfirmRoutineProposalCommand(
        Guid ProposalId,
        string? ProposalHash)
        : IRequest<RoutinePlannerConfirmationDto>;
}
