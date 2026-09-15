using Entrenate.Application.Routines.Models;
using MediatR;

namespace Entrenate.Application.Routines.Commands.UpdateRoutine
{
    public record UpdateRoutineCommand(
        Guid Id,
        string Nombre,
        string? Descripcion,
        List<RoutineDayInput> Dias)
    : IRequest;
}
