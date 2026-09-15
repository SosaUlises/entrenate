using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using Entrenate.Application.Routines.DTOs;
using MediatR;

namespace Entrenate.Application.Routines.Queries.GetRoutineById
{
    public class GetRoutineByIdQueryHandler
    : IRequestHandler<GetRoutineByIdQuery, RoutineDto>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IRoutineRepository _routineRepository;

        public GetRoutineByIdQueryHandler(
            ICurrentUserService currentUserService,
            IRoutineRepository routineRepository)
        {
            _currentUserService = currentUserService;
            _routineRepository = routineRepository;
        }

        public async Task<RoutineDto> Handle(
            GetRoutineByIdQuery request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException(
                    "No se pudo identificar al usuario autenticado.");
            }

            var routine = await _routineRepository
                .GetDetailByIdAndUserIdAsync(
                    request.Id,
                    userId,
                    cancellationToken);

            if (routine is null)
            {
                throw new NotFoundException("La rutina no existe.");
            }

            return new RoutineDto(
                routine.Id,
                routine.Nombre,
                routine.Descripcion,
                routine.Activa,
                routine.FechaCreacion,
                routine.FechaUltimaModificacion,
                routine.Dias
                    .OrderBy(x => x.Orden)
                    .Select(day => new RoutineDayDto(
                        day.Id,
                        day.Nombre,
                        day.Descripcion,
                        day.Orden,
                        day.Ejercicios
                            .OrderBy(x => x.Orden)
                            .Select(exercise => new RoutineExerciseDto(
                                exercise.EjercicioId,
                                exercise.Ejercicio.Nombre,
                                exercise.CantidadSeries,
                                exercise.RepeticionesMinimas,
                                exercise.RepeticionesMaximas,
                                exercise.RirObjetivoMinimo,
                                exercise.RirObjetivoMaximo,
                                exercise.DescansoSegundos,
                                exercise.Orden,
                                exercise.Notas))
                            .ToList()))
                    .ToList());
        }
    }
}
