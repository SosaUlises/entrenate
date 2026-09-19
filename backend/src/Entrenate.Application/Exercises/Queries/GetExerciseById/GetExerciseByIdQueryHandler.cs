using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using Entrenate.Application.Exercises.DTOs;
using MediatR;

namespace Entrenate.Application.Exercises.Queries.GetExerciseById
{
    public class GetExerciseByIdQueryHandler
    : IRequestHandler<GetExerciseByIdQuery, ExerciseDto>
    {
        private readonly IExerciseRepository _exerciseRepository;
        private readonly IEquipmentRepository _equipmentRepository;

        public GetExerciseByIdQueryHandler(
            IExerciseRepository exerciseRepository,
            IEquipmentRepository equipmentRepository)
        {
            _exerciseRepository = exerciseRepository;
            _equipmentRepository = equipmentRepository;
        }

        public async Task<ExerciseDto> Handle(
            GetExerciseByIdQuery request,
            CancellationToken cancellationToken)
        {
            var exercise = await _exerciseRepository.GetActiveByIdAsync(
                request.Id,
                cancellationToken);

            if (exercise is null)
            {
                throw new NotFoundException(
                    "El ejercicio no existe o no está disponible.");
            }

            var equipment = await _equipmentRepository.GetByIdsAsync(
                exercise.Equipamientos.Select(x => x.EquipamientoId),
                cancellationToken);

            return new ExerciseDto(
                exercise.Id,
                exercise.Nombre,
                exercise.Descripcion,
                exercise.GrupoMuscularPrincipal,
                equipment
                    .Select(x => new ExerciseEquipmentDto(
                        x.Id,
                        x.Nombre,
                        x.Categoria))
                    .ToList(),
                exercise.WorkoutGuideId);
        }
    }
}
