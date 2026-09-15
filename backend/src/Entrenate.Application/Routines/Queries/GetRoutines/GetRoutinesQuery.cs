using Entrenate.Application.Routines.DTOs;
using MediatR;

namespace Entrenate.Application.Routines.Queries.GetRoutines
{
    public record GetRoutinesQuery
    : IRequest<IReadOnlyCollection<RoutineSummaryDto>>;
}
