using Entrenate.Application.Coach.AdaptiveTraining.TimeAdaptation;
using Entrenate.Domain.Enums;

namespace Entrenate.Application.Coach.AdaptiveTraining.Lighter
{
    public static class AdaptiveTrainingLighterEngine
    {
        public const string PolicyVersion = "3.0-at3";

        private const int PreparationSeconds = 5 * 60;
        private const int TransitionSeconds = 60;
        private const int SetExecutionSeconds = 45;
        private const int MaxRirCap = 5;

        public static AdaptiveTrainingLighterProposal Adapt(
            PlannedLighterSession session)
        {
            ArgumentNullException.ThrowIfNull(session);

            Validate(session);

            var orderedExercises = session.Exercises
                .OrderBy(exercise => exercise.Order)
                .ThenBy(
                    exercise => exercise.ExerciseCode,
                    StringComparer.Ordinal)
                .ThenBy(exercise => exercise.ExerciseId)
                .ToArray();

            var originalSnapshots = orderedExercises
                .Select(exercise => ToSnapshot(
                    exercise,
                    DeterminePriority(exercise, orderedExercises)))
                .ToArray();

            var originalDurationSeconds = CalculateDurationSeconds(
                originalSnapshots);

            var originalSession = new AdaptiveTrainingLighterSessionSnapshot(
                session.RoutineDayId,
                originalDurationSeconds,
                originalSnapshots);

            var adaptedSnapshots = new List<AdaptiveTrainingLighterExerciseSnapshot>();
            var changes = new List<AdaptiveTrainingLighterChange>();
            var diagnostics = new List<AdaptiveTrainingLighterDiagnostic>();

            foreach (var original in originalSnapshots)
            {
                var (adaptedMinRir, adaptedMaxRir) = CalculateLighterRir(
                    original.Prescription.MinRir,
                    original.Prescription.MaxRir);

                var rirChanged = adaptedMinRir != original.Prescription.MinRir ||
                    adaptedMaxRir != original.Prescription.MaxRir;

                if (rirChanged)
                {
                    changes.Add(new AdaptiveTrainingLighterChange(
                        AdaptiveTrainingLighterChangeType.RirIncreased,
                        original.SourceRoutineExerciseId,
                        original.ExerciseId,
                        original.ExerciseCode,
                        OriginalMinRir: original.Prescription.MinRir,
                        OriginalMaxRir: original.Prescription.MaxRir,
                        AdaptedMinRir: adaptedMinRir,
                        AdaptedMaxRir: adaptedMaxRir));

                    diagnostics.Add(new AdaptiveTrainingLighterDiagnostic(
                        AdaptiveTrainingLighterDiagnosticCode.RirIncreased,
                        original.SourceRoutineExerciseId,
                        original.ExerciseId,
                        original.ExerciseCode));
                }

                var adaptedSets = original.Prescription.Sets;
                var canReduceSets = original.Priority == AdaptiveTrainingExercisePriority.OptionalAccessory &&
                    original.Prescription.Sets > 1;

                if (canReduceSets)
                {
                    adaptedSets = original.Prescription.Sets - 1;

                    changes.Add(new AdaptiveTrainingLighterChange(
                        AdaptiveTrainingLighterChangeType.OptionalAccessorySetsReduced,
                        original.SourceRoutineExerciseId,
                        original.ExerciseId,
                        original.ExerciseCode,
                        OriginalSets: original.Prescription.Sets,
                        AdaptedSets: adaptedSets));

                    diagnostics.Add(new AdaptiveTrainingLighterDiagnostic(
                        AdaptiveTrainingLighterDiagnosticCode.OptionalAccessorySetsReduced,
                        original.SourceRoutineExerciseId,
                        original.ExerciseId,
                        original.ExerciseCode));
                }

                var adaptedPrescription = new AdaptiveTrainingLighterPrescription(
                    adaptedSets,
                    original.Prescription.MinReps,
                    original.Prescription.MaxReps,
                    adaptedMinRir,
                    adaptedMaxRir,
                    original.Prescription.RestSeconds,
                    original.Prescription.Notes);

                adaptedSnapshots.Add(new AdaptiveTrainingLighterExerciseSnapshot(
                    original.SourceRoutineExerciseId,
                    original.ExerciseId,
                    original.ExerciseCode,
                    original.Name,
                    original.Order,
                    original.Priority,
                    original.MovementPattern,
                    original.ExerciseType,
                    original.PrimaryMuscleGroup,
                    original.SecondaryMuscleGroups,
                    original.RequiredEquipmentIds,
                    adaptedPrescription));
            }

            var adaptedDurationSeconds = CalculateDurationSeconds(
                adaptedSnapshots);

            if (changes.Count == 0)
            {
                diagnostics.Add(new AdaptiveTrainingLighterDiagnostic(
                    AdaptiveTrainingLighterDiagnosticCode.NeutralLighterTransformation));
            }

            var feasibility = changes.Count > 0
                ? AdaptiveTrainingLighterFeasibility.FeasibleWithChanges
                : AdaptiveTrainingLighterFeasibility.Feasible;

            var adaptedSession = new AdaptiveTrainingLighterSessionSnapshot(
                session.RoutineDayId,
                adaptedDurationSeconds,
                adaptedSnapshots);

            return new AdaptiveTrainingLighterProposal(
                PolicyVersion,
                session.RoutineDayId,
                feasibility,
                originalSession,
                adaptedSession,
                changes,
                diagnostics);
        }

        public static (int? MinRir, int? MaxRir) CalculateLighterRir(
            int? minRir,
            int? maxRir)
        {
            if (!minRir.HasValue && !maxRir.HasValue)
            {
                return (null, null);
            }

            int? newMin = minRir.HasValue
                ? Math.Min(MaxRirCap, minRir.Value + 1)
                : null;

            int? newMax = maxRir.HasValue
                ? Math.Min(MaxRirCap, maxRir.Value + 1)
                : null;

            if (newMin.HasValue && newMax.HasValue && newMin.Value > newMax.Value)
            {
                newMin = newMax;
            }

            return (newMin, newMax);
        }

        private static int CalculateDurationSeconds(
            IReadOnlyCollection<AdaptiveTrainingLighterExerciseSnapshot> exercises)
        {
            var values = exercises
                .Select(e => (e.Prescription.Sets, e.Prescription.RestSeconds))
                .ToArray();

            var transitionCount = Math.Max(0, values.Length - 1);

            return checked(
                PreparationSeconds +
                transitionCount * TransitionSeconds +
                values.Sum(value =>
                    SetExecutionSeconds * value.Sets +
                    value.RestSeconds * (value.Sets - 1)));
        }

        private static AdaptiveTrainingExercisePriority DeterminePriority(
            PlannedLighterExercise exercise,
            IReadOnlyCollection<PlannedLighterExercise> sessionExercises)
        {
            if (!HasCompleteClassificationMetadata(exercise))
            {
                return AdaptiveTrainingExercisePriority.UnclassifiedRequired;
            }

            if (exercise.ExerciseType == TipoEjercicio.Compuesto)
            {
                return AdaptiveTrainingExercisePriority.RequiredCompound;
            }

            if (exercise.IsRequiredForAdaptation.HasValue)
            {
                return exercise.IsRequiredForAdaptation.Value
                    ? AdaptiveTrainingExercisePriority.RequiredIsolation
                    : AdaptiveTrainingExercisePriority.OptionalAccessory;
            }

            var samePatternCount = sessionExercises.Count(candidate =>
                candidate.MovementPattern == exercise.MovementPattern);
            var samePrimaryMuscleCount = sessionExercises.Count(candidate =>
                string.Equals(
                    candidate.PrimaryMuscleGroup?.Trim(),
                    exercise.PrimaryMuscleGroup.Trim(),
                    StringComparison.OrdinalIgnoreCase));

            return samePatternCount == 1 || samePrimaryMuscleCount == 1
                ? AdaptiveTrainingExercisePriority.RequiredIsolation
                : AdaptiveTrainingExercisePriority.OptionalAccessory;
        }

        private static bool HasCompleteClassificationMetadata(
            PlannedLighterExercise exercise)
        {
            return exercise.MovementPattern.HasValue &&
                Enum.IsDefined(exercise.MovementPattern.Value) &&
                exercise.ExerciseType.HasValue &&
                Enum.IsDefined(exercise.ExerciseType.Value) &&
                !string.IsNullOrWhiteSpace(exercise.PrimaryMuscleGroup);
        }

        private static AdaptiveTrainingLighterExerciseSnapshot ToSnapshot(
            PlannedLighterExercise exercise,
            AdaptiveTrainingExercisePriority priority)
        {
            return new AdaptiveTrainingLighterExerciseSnapshot(
                exercise.SourceRoutineExerciseId,
                exercise.ExerciseId,
                exercise.ExerciseCode,
                exercise.Name,
                exercise.Order,
                priority,
                exercise.MovementPattern,
                exercise.ExerciseType,
                exercise.PrimaryMuscleGroup,
                NormalizeSecondaryMuscleGroups(exercise.SecondaryMuscleGroups),
                exercise.RequiredEquipmentIds?.ToArray() ?? [],
                exercise.Prescription);
        }

        private static IReadOnlyCollection<string> NormalizeSecondaryMuscleGroups(
            IEnumerable<string>? groups)
        {
            if (groups is null)
            {
                return [];
            }

            return groups
                .Where(group => !string.IsNullOrWhiteSpace(group))
                .Select(group => group.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(group => group, StringComparer.Ordinal)
                .ToArray();
        }

        private static void Validate(PlannedLighterSession session)
        {
            if (session.RoutineDayId == Guid.Empty)
            {
                throw new ArgumentException(
                    "RoutineDayId must be provided.",
                    nameof(session));
            }

            if (session.Exercises is null || session.Exercises.Count == 0)
            {
                throw new ArgumentException(
                    "A session must contain at least one exercise.",
                    nameof(session));
            }

            var routineExerciseIds = new HashSet<Guid>();

            foreach (var exercise in session.Exercises)
            {
                if (exercise.SourceRoutineExerciseId == Guid.Empty)
                {
                    throw new ArgumentException(
                        "SourceRoutineExerciseId must be provided.",
                        nameof(session));
                }

                if (exercise.ExerciseId == Guid.Empty)
                {
                    throw new ArgumentException(
                        "ExerciseId must be provided.",
                        nameof(session));
                }

                if (!routineExerciseIds.Add(exercise.SourceRoutineExerciseId))
                {
                    throw new ArgumentException(
                        $"Duplicate SourceRoutineExerciseId: {exercise.SourceRoutineExerciseId}",
                        nameof(session));
                }

                if (exercise.Prescription is null)
                {
                    throw new ArgumentException(
                        $"Prescription is required for exercise {exercise.ExerciseId}.",
                        nameof(session));
                }

                if (exercise.Prescription.Sets <= 0)
                {
                    throw new ArgumentException(
                        $"Sets must be greater than zero for exercise {exercise.ExerciseId}.",
                        nameof(session));
                }

                if (exercise.Prescription.MinReps <= 0)
                {
                    throw new ArgumentException(
                        $"MinReps must be greater than zero for exercise {exercise.ExerciseId}.",
                        nameof(session));
                }

                if (exercise.Prescription.MinReps > exercise.Prescription.MaxReps)
                {
                    throw new ArgumentException(
                        $"MinReps ({exercise.Prescription.MinReps}) cannot exceed MaxReps ({exercise.Prescription.MaxReps}) for exercise {exercise.ExerciseId}.",
                        nameof(session));
                }

                if (exercise.Prescription.MinRir is < 0 or > 5)
                {
                    throw new ArgumentException(
                        $"MinRir ({exercise.Prescription.MinRir}) must be between 0 and 5 for exercise {exercise.ExerciseId}.",
                        nameof(session));
                }

                if (exercise.Prescription.MaxRir is < 0 or > 5)
                {
                    throw new ArgumentException(
                        $"MaxRir ({exercise.Prescription.MaxRir}) must be between 0 and 5 for exercise {exercise.ExerciseId}.",
                        nameof(session));
                }

                if (exercise.Prescription.MinRir.HasValue &&
                    exercise.Prescription.MaxRir.HasValue &&
                    exercise.Prescription.MinRir.Value > exercise.Prescription.MaxRir.Value)
                {
                    throw new ArgumentException(
                        $"MinRir ({exercise.Prescription.MinRir}) cannot exceed MaxRir ({exercise.Prescription.MaxRir}) for exercise {exercise.ExerciseId}.",
                        nameof(session));
                }

                if (exercise.Prescription.RestSeconds < 0)
                {
                    throw new ArgumentException(
                        $"RestSeconds must be non-negative for exercise {exercise.ExerciseId}.",
                        nameof(session));
                }
            }
        }
    }
}

