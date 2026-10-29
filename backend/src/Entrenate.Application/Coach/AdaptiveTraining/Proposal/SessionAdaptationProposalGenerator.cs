using Entrenate.Application.Coach.AdaptiveTraining.Substitution;
using Entrenate.Application.Coach.AdaptiveTraining.TimeAdaptation;
using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using Entrenate.Domain.Entidades;
using FluentValidation;
using FluentValidation.Results;

namespace Entrenate.Application.Coach.AdaptiveTraining.Proposal
{
    public sealed class SessionAdaptationProposalGenerator
    {
        private readonly IRoutineRepository _routineRepository;
        private readonly ITrainingProfileRepository _profileRepository;
        private readonly IExerciseRepository _exerciseRepository;

        public SessionAdaptationProposalGenerator(
            IRoutineRepository routineRepository,
            ITrainingProfileRepository profileRepository,
            IExerciseRepository exerciseRepository)
        {
            _routineRepository = routineRepository;
            _profileRepository = profileRepository;
            _exerciseRepository = exerciseRepository;
        }

        public async Task<SessionAdaptationProposal> GenerateForUserAsync(
            string userId,
            Guid routineDayId,
            int? availableMinutes,
            IReadOnlyCollection<Guid>? availableEquipmentIds,
            IReadOnlyCollection<Guid>? excludedExerciseIds,
            SessionEffortPreference effortPreference,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException(
                    "No se pudo identificar al usuario autenticado.");
            }

            var routineDay = await _routineRepository.GetDayByIdAndUserIdAsync(
                routineDayId,
                userId,
                cancellationToken);

            if (routineDay is null)
            {
                throw new NotFoundException(
                    "El día de rutina no existe.");
            }

            if (!routineDay.Rutina.Activa)
            {
                throw new ConflictException(
                    "La rutina debe estar activa para poder adaptar el entrenamiento.");
            }

            var profile = await _profileRepository.GetByUserIdAsync(
                userId,
                cancellationToken);

            if (profile is null)
            {
                throw new NotFoundException(
                    "El usuario todavía no tiene un perfil de entrenamiento.");
            }

            var effectiveAvailableEquipmentIds = availableEquipmentIds is not null
                ? availableEquipmentIds.Distinct().ToArray()
                : profile.Equipamientos.Select(e => e.EquipamientoId).Distinct().ToArray();

            var normalizedExcluded = excludedExerciseIds?.Distinct().ToArray() ?? [];
            var dayExerciseIds = routineDay.Ejercicios
                .Select(e => e.EjercicioId)
                .ToHashSet();

            foreach (var excludedId in normalizedExcluded)
            {
                if (!dayExerciseIds.Contains(excludedId))
                {
                    throw new ValidationException(new[]
                    {
                        new ValidationFailure(
                            nameof(excludedExerciseIds),
                            "Los ejercicios excluidos deben pertenecer al día de rutina.")
                    });
                }
            }

            var activeExercises = await _exerciseRepository.GetActiveAsync(cancellationToken);
            var activeById = activeExercises.ToDictionary(e => e.Id);
            var catalog = activeExercises.Select(MapCandidate).ToArray();

            var plannedExercises = routineDay.Ejercicios
                .OrderBy(e => e.Orden)
                .Select(e =>
                {
                    activeById.TryGetValue(e.EjercicioId, out var exercise);
                    var codigo = exercise?.Codigo ?? e.Ejercicio?.Codigo ?? string.Empty;
                    var nombre = exercise?.Nombre ?? e.Ejercicio?.Nombre ?? string.Empty;
                    var patron = exercise?.PatronMovimiento ?? e.Ejercicio?.PatronMovimiento;
                    var tipo = exercise?.TipoEjercicio ?? e.Ejercicio?.TipoEjercicio;
                    var grupoPrincipal = (exercise?.GrupoMuscularPrincipal ?? e.Ejercicio?.GrupoMuscularPrincipal ?? "Pecho").ToString();
                    var gruposSecundarios = (exercise?.GruposMuscularesSecundarios?.Select(g => g.GrupoMuscular.ToString())
                        ?? e.Ejercicio?.GruposMuscularesSecundarios?.Select(g => g.GrupoMuscular.ToString())
                        ?? []).ToArray();
                    var equipamientosRequeridos = (exercise?.Equipamientos?.Select(eq => eq.EquipamientoId)
                        ?? e.Ejercicio?.Equipamientos?.Select(eq => eq.EquipamientoId)
                        ?? []).ToArray();

                    return new PlannedAdaptiveExercise(
                        e.Id,
                        e.EjercicioId,
                        codigo,
                        nombre,
                        e.Orden,
                        patron,
                        tipo,
                        grupoPrincipal,
                        gruposSecundarios,
                        equipamientosRequeridos,
                        e.IsRequiredForAdaptation,
                        new AdaptiveTrainingPrescription(
                            e.CantidadSeries,
                            e.RepeticionesMinimas,
                            e.RepeticionesMaximas,
                            e.RirObjetivoMinimo,
                            e.RirObjetivoMaximo,
                            e.DescansoSegundos,
                            e.Notas));
                })
                .ToArray();

            var plannedSession = new PlannedAdaptiveSession(
                routineDay.Id,
                plannedExercises,
                routineDay.RutinaId,
                routineDay.Rutina.FechaUltimaModificacion);

            var adaptationRequest = new SessionAdaptationRequest(
                routineDay.Id,
                availableMinutes,
                effectiveAvailableEquipmentIds,
                normalizedExcluded,
                effortPreference);

            return SessionAdaptationProposalEngine.Adapt(
                plannedSession,
                adaptationRequest,
                catalog);
        }

        private static AdaptiveSubstitutionCandidate MapCandidate(Ejercicio exercise)
        {
            return new AdaptiveSubstitutionCandidate(
                exercise.Id,
                exercise.Codigo,
                exercise.Nombre,
                exercise.PatronMovimiento,
                exercise.TipoEjercicio,
                exercise.GrupoMuscularPrincipal.ToString(),
                exercise.GruposMuscularesSecundarios.Select(g => g.GrupoMuscular.ToString()).ToArray(),
                exercise.Activo,
                exercise.Equipamientos.Select(e => e.EquipamientoId).ToArray());
        }
    }
}
