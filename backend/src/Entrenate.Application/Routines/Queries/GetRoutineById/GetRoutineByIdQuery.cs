using Entrenate.Application.Routines.DTOs;
using MediatR;

namespace Entrenate.Application.Routines.Queries.GetRoutineById
{
    public record GetRoutineByIdQuery(Guid Id)
    : IRequest<RoutineDto>;
}
