using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using MediatR;

namespace Entrenate.Application.Routines.Commands.DeleteRoutine
{
    public class DeleteRoutineCommandHandler
        : IRequestHandler<DeleteRoutineCommand>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IRoutineRepository _routineRepository;

        public DeleteRoutineCommandHandler(
            ICurrentUserService currentUserService,
            IRoutineRepository routineRepository)
        {
            _currentUserService = currentUserService;
            _routineRepository = routineRepository;
        }

        public async Task Handle(
            DeleteRoutineCommand request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException(
                    "No se pudo identificar al usuario autenticado.");
            }

            var routine = await _routineRepository
                .GetForUpdateByIdAndUserIdAsync(
                    request.Id,
                    userId,
                    cancellationToken);

            if (routine is null)
            {
                throw new NotFoundException("La rutina no existe.");
            }

            if (await _routineRepository.HasActiveSessionAsync(
                routine.Id,
                userId,
                cancellationToken))
            {
                throw new ConflictException(
                    "No se puede eliminar la rutina mientras existe " +
                    "un entrenamiento en curso asociado.");
            }

            _routineRepository.Remove(routine);
            await _routineRepository.SaveChangesAsync(cancellationToken);
        }
    }
}
