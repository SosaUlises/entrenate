using Entrenate.Application.Common.Interfaces;
using Entrenate.Application.Exercises.DTOs;
using MediatR;

namespace Entrenate.Application.Exercises.Queries.GetExercises
{
    public class GetExercisesQueryHandler
    : IRequestHandler<
        GetExercisesQuery,
        IReadOnlyCollection<ExerciseDto>>
    {
        private readonly IExerciseRepository _exerciseRepository;
        private readonly IEquipmentRepository _equipmentRepository;

        public GetExercisesQueryHandler(
            IExerciseRepository exerciseRepository,
            IEquipmentRepository equipmentRepository)
        {
            _exerciseRepository = exerciseRepository;
            _equipmentRepository = equipmentRepository;
        }

        public async Task<IReadOnlyCollection<ExerciseDto>> Handle(
            GetExercisesQuery request,
            CancellationToken cancellationToken)
        {
            var exercises = await _exerciseRepository.GetActiveAsync(
                cancellationToken);

            var equipmentIds = exercises
                .SelectMany(x => x.Equipamientos)
                .Select(x => x.EquipamientoId)
                .Distinct()
                .ToList();

            var equipment = await _equipmentRepository.GetByIdsAsync(
                equipmentIds,
                cancellationToken);

            var equipmentById = equipment.ToDictionary(x => x.Id);

            return exercises
                .Select(exercise => new ExerciseDto(
                    exercise.Id,
                    exercise.Nombre,
                    exercise.Descripcion,
                    exercise.GrupoMuscularPrincipal,
                    exercise.Equipamientos
                        .Select(x => equipmentById[x.EquipamientoId])
                        .OrderBy(x => x.Categoria)
                        .ThenBy(x => x.Nombre)
                        .Select(x => new ExerciseEquipmentDto(
                            x.Id,
                            x.Nombre,
                            x.Categoria))
                        .ToList()))
                .ToList();
        }
    }
}
