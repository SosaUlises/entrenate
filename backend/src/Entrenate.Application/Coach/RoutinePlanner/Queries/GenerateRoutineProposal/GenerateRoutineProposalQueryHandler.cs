using Entrenate.Application.Coach.RoutinePlanner.DTOs;
using Entrenate.Application.Coach.RoutinePlanner.ExerciseSelection;
using Entrenate.Application.Coach.RoutinePlanner.Proposal;
using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using Entrenate.Domain.Entidades;
using MediatR;

namespace Entrenate.Application.Coach.RoutinePlanner.Queries.GenerateRoutineProposal
{
    public sealed class GenerateRoutineProposalQueryHandler
        : IRequestHandler<
            GenerateRoutineProposalQuery,
            RoutinePlannerProposalDto>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly ITrainingProfileRepository _profileRepository;
        private readonly IExerciseRepository _exerciseRepository;
        private readonly IEquipmentRepository _equipmentRepository;

        public GenerateRoutineProposalQueryHandler(
            ICurrentUserService currentUserService,
            ITrainingProfileRepository profileRepository,
            IExerciseRepository exerciseRepository,
            IEquipmentRepository equipmentRepository)
        {
            _currentUserService = currentUserService;
            _profileRepository = profileRepository;
            _exerciseRepository = exerciseRepository;
            _equipmentRepository = equipmentRepository;
        }

        public async Task<RoutinePlannerProposalDto> Handle(
            GenerateRoutineProposalQuery request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException(
                    "No se pudo identificar al usuario autenticado.");
            }

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
            var declaredEquipmentIds = profile.Equipamientos
                .Select(item => item.EquipamientoId)
                .ToArray();
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
                declaredEquipmentIds,
                catalog);
            var proposal = RoutinePlannerProposalBuilder.Build(input);

            return MapProposal(proposal);
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

        private static RoutinePlannerProposalDto MapProposal(
            RoutinePlannerProposal proposal)
        {
            return new RoutinePlannerProposalDto(
                proposal.ProposalId,
                proposal.PolicyVersion,
                proposal.CanonicalHash,
                proposal.Feasibility.ToString(),
                proposal.Goal.ToString(),
                proposal.ExperienceLevel.ToString(),
                proposal.RequestedDaysPerWeek,
                proposal.Days
                    .Select(day => new RoutinePlannerProposalDayDto(
                        day.Order,
                        day.ScheduledDay.ToString(),
                        day.DayOrigin.ToString(),
                        day.Focus.ToString(),
                        day.Variant.ToString(),
                        day.TargetDurationMinutes,
                        day.EstimatedDurationSeconds,
                        day.Exercises
                            .Select(exercise =>
                                new RoutinePlannerProposalExerciseDto(
                                    exercise.ExerciseId,
                                    exercise.Code,
                                    exercise.Name,
                                    exercise.Order,
                                    exercise.MovementPattern.ToString(),
                                    exercise.ExerciseType.ToString(),
                                    exercise.Role.ToString(),
                                    exercise.Sets,
                                    exercise.MinReps,
                                    exercise.MaxReps,
                                    exercise.MinRir,
                                    exercise.MaxRir,
                                    exercise.RestSeconds))
                            .ToArray()))
                    .ToArray(),
                proposal.WeeklyMuscleVolumes
                    .Select(volume =>
                        new RoutinePlannerWeeklyVolumeDto(
                            volume.MuscleGroup,
                            volume.EffectiveSets))
                    .ToArray(),
                proposal.SelectionDiagnostics
                    .Select(diagnostic =>
                        new RoutinePlannerSelectionDiagnosticDto(
                            diagnostic.Code.ToString(),
                            diagnostic.DayOrder,
                            diagnostic.SlotPosition))
                    .ToArray(),
                proposal.PrescriptionDiagnostics
                    .Select(diagnostic =>
                        new RoutinePlannerPrescriptionDiagnosticDto(
                            diagnostic.Code.ToString(),
                            diagnostic.DayOrder,
                            diagnostic.ExerciseCode,
                            diagnostic.MovementPattern?.ToString(),
                            diagnostic.MuscleGroup))
                    .ToArray());
        }
    }
}
