using MediatR;

namespace Entrenate.Application.Routines.Commands.DeleteRoutine
{
    public record DeleteRoutineCommand(Guid Id) : IRequest;
}
