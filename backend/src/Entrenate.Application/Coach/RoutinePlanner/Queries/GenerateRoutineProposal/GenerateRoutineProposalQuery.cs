using Entrenate.Application.Coach.RoutinePlanner.DTOs;
using MediatR;

namespace Entrenate.Application.Coach.RoutinePlanner.Queries.GenerateRoutineProposal
{
    public sealed record GenerateRoutineProposalQuery
        : IRequest<RoutinePlannerProposalDto>;
}
