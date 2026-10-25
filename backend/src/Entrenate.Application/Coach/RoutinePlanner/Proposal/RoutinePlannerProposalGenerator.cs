using Entrenate.Application.Coach.RoutinePlanner.ExerciseSelection;
using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using Entrenate.Domain.Entidades;

namespace Entrenate.Application.Coach.RoutinePlanner.Proposal
{
    public sealed class RoutinePlannerProposalGenerator
    {
        private readonly ITrainingProfileRepository _profileRepository;
        private readonly IExerciseRepository _exerciseRepository;
        private readonly IEquipmentRepository _equipmentRepository;

        public RoutinePlannerProposalGenerator(
            ITrainingProfileRepository profileRepository,
            IExerciseRepository exerciseRepository,
            IEquipmentRepository equipmentRepository)
        {
            _profileRepository = profileRepository;
            _exerciseRepository = exerciseRepository;
            _equipmentRepository = equipmentRepository;
        }

        public async Task<RoutinePlannerProposal> GenerateForUserAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            var profile = await _profileRepository.GetByUserIdAsync(
                userId,
                cancellationToken);

            if (profile is null)
            {
                throw new NotFoundException(
                    "El usuario todavía no tiene un perfil de entrenamiento.");
            }

            var exercises = await _exerciseRepository.GetActiveAsync(
                cancellationToken);
            var equipmentIds = profile.Equipamientos
                .Select(item => item.EquipamientoId)
                .Concat(exercises
                    .SelectMany(exercise => exercise.Equipamientos)
                    .Select(item => item.EquipamientoId))
                .Distinct()
                .ToArray();
            var equipment = await _equipmentRepository.GetByIdsAsync(
                equipmentIds,
                cancellationToken);
            var activeEquipmentIds = equipment
                .Where(item => item.Activo)
                .Select(item => item.Id)
                .ToHashSet();
            var catalog = exercises
                .Select(exercise => MapCandidate(
                    exercise,
                    activeEquipmentIds))
                .ToArray();
            var input = new RoutinePlannerInput(
                RoutinePlannerProposalBuilder.PolicyVersion,
                profile.Objetivo,
                profile.NivelExperiencia,
                profile.DiasEntrenamientoPorSemana,
                profile.DiasPreferidos
                    .Select(day => day.Dia)
                    .ToArray(),
                profile.DuracionSesionMinutos,
                profile.Equipamientos
                    .Select(item => item.EquipamientoId)
                    .ToArray(),
                catalog);

            return RoutinePlannerProposalBuilder.Build(input);
        }

        private static PlannerExerciseCandidate MapCandidate(
            Ejercicio exercise,
            IReadOnlySet<Guid> activeEquipmentIds)
        {
            return new PlannerExerciseCandidate(
                exercise.Id,
                exercise.Codigo,
                exercise.Nombre,
                exercise.GrupoMuscularPrincipal,
                exercise.GruposMuscularesSecundarios
                    .Select(group => group.GrupoMuscular)
                    .ToArray(),
                exercise.PatronMovimiento,
                exercise.TipoEjercicio,
                exercise.Activo,
                exercise.Equipamientos
                    .Select(requirement =>
                        new ExerciseEquipmentRequirement(
                            requirement.EquipamientoId,
                            activeEquipmentIds.Contains(
                                requirement.EquipamientoId)))
                    .ToArray());
        }
    }
}
