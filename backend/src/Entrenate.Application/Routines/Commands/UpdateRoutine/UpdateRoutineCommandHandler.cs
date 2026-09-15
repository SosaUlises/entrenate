using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace Entrenate.Application.Routines.Commands.UpdateRoutine
{
    public class UpdateRoutineCommandHandler
    : IRequestHandler<UpdateRoutineCommand>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IRoutineRepository _routineRepository;
        private readonly IExerciseRepository _exerciseRepository;

        public UpdateRoutineCommandHandler(
            ICurrentUserService currentUserService,
            IRoutineRepository routineRepository,
            IExerciseRepository exerciseRepository)
        {
            _currentUserService = currentUserService;
            _routineRepository = routineRepository;
            _exerciseRepository = exerciseRepository;
        }

        public async Task Handle(
            UpdateRoutineCommand request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException(
                    "No se pudo identificar al usuario autenticado.");
            }

            var rutina = await _routineRepository
                .GetForUpdateByIdAndUserIdAsync(
                    request.Id,
                    userId,
                    cancellationToken);

            if (rutina is null)
            {
                throw new NotFoundException("La rutina no existe.");
            }

            await ValidateExerciseIdsAsync(
                request,
                cancellationToken);

            rutina.Actualizar(
                request.Nombre,
                request.Descripcion);

            rutina.EliminarDiasExcepto(
                request.Dias.Select(x => x.Orden));

            foreach (var dayInput in request.Dias)
            {
                var dia = rutina.Dias.SingleOrDefault(
                    x => x.Orden == dayInput.Orden);

                if (dia is null)
                {
                    dia = rutina.AgregarDia(
                        dayInput.Nombre,
                        dayInput.Descripcion,
                        dayInput.Orden);
                }
                else
                {
                    dia.Actualizar(
                        dayInput.Nombre,
                        dayInput.Descripcion);
                }

                dia.EliminarEjerciciosExcepto(
                    dayInput.Ejercicios.Select(x => x.Orden));

                foreach (var exerciseInput in dayInput.Ejercicios)
                {
                    var ejercicio = dia.Ejercicios.SingleOrDefault(
                        x => x.Orden == exerciseInput.Orden);

                    if (ejercicio is null)
                    {
                        dia.AgregarEjercicio(
                            exerciseInput.EjercicioId,
                            exerciseInput.CantidadSeries,
                            exerciseInput.RepeticionesMinimas,
                            exerciseInput.RepeticionesMaximas,
                            exerciseInput.RirObjetivoMinimo,
                            exerciseInput.RirObjetivoMaximo,
                            exerciseInput.DescansoSegundos,
                            exerciseInput.Orden,
                            exerciseInput.Notas);

                        continue;
                    }

                    ejercicio.Actualizar(
                        exerciseInput.EjercicioId,
                        exerciseInput.CantidadSeries,
                        exerciseInput.RepeticionesMinimas,
                        exerciseInput.RepeticionesMaximas,
                        exerciseInput.RirObjetivoMinimo,
                        exerciseInput.RirObjetivoMaximo,
                        exerciseInput.DescansoSegundos,
                        exerciseInput.Orden,
                        exerciseInput.Notas);
                }
            }

            await _routineRepository.SaveChangesAsync(cancellationToken);
        }

        private async Task ValidateExerciseIdsAsync(
            UpdateRoutineCommand request,
            CancellationToken cancellationToken)
        {
            var requestedIds = request.Dias
                .SelectMany(x => x.Ejercicios)
                .Select(x => x.EjercicioId)
                .Distinct()
                .ToList();

            var validIds = await _exerciseRepository
                .GetValidActiveIdsAsync(
                    requestedIds,
                    cancellationToken);

            if (validIds.Count != requestedIds.Count)
            {
                throw new ValidationException(
                    new[]
                    {
                        new ValidationFailure(
                            nameof(request.Dias),
                            "Uno o más ejercicios no existen o " +
                            "no están disponibles.")
                    });
            }
        }
    }
}
