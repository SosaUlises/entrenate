using Entrenate.Application.Common.Interfaces;
using Entrenate.Domain.Entidades;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace Entrenate.Application.Routines.Commands.CreateRoutine
{
    public class CreateRoutineCommandHandler
    : IRequestHandler<CreateRoutineCommand, Guid>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IRoutineRepository _routineRepository;
        private readonly IExerciseRepository _exerciseRepository;

        public CreateRoutineCommandHandler(
            ICurrentUserService currentUserService,
            IRoutineRepository routineRepository,
            IExerciseRepository exerciseRepository)
        {
            _currentUserService = currentUserService;
            _routineRepository = routineRepository;
            _exerciseRepository = exerciseRepository;
        }

        public async Task<Guid> Handle(
            CreateRoutineCommand request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException(
                    "No se pudo identificar al usuario autenticado.");
            }

            await ValidateExerciseIdsAsync(
                request,
                cancellationToken);

            var rutina = new Rutina(
                userId,
                request.Nombre,
                request.Descripcion);

            foreach (var dayInput in request.Dias)
            {
                var dia = rutina.AgregarDia(
                    dayInput.Nombre,
                    dayInput.Descripcion,
                    dayInput.Orden);

                foreach (var exerciseInput in dayInput.Ejercicios)
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
                }
            }

            await _routineRepository.AddAsync(
                rutina,
                cancellationToken);

            await _routineRepository.SaveChangesAsync(cancellationToken);

            return rutina.Id;
        }

        private async Task ValidateExerciseIdsAsync(
            CreateRoutineCommand request,
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
