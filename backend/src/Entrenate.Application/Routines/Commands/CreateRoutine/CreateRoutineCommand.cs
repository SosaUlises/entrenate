using Entrenate.Application.Routines.Models;
using MediatR;

namespace Entrenate.Application.Routines.Commands.CreateRoutine
{
    public record CreateRoutineCommand(
        string Nombre,
        string? Descripcion,
        List<RoutineDayInput> Dias)
    : IRequest<Guid>;
}
