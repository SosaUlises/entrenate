using Entrenate.Domain.Enums;

namespace Entrenate.Application.Coach.AdaptiveTraining.TimeAdaptation
{
    public static class AdaptiveTrainingTimeEngine
    {
        public const string PolicyVersion = "3.0-at1";

        private const int PreparationSeconds = 5 * 60;
        private const int TransitionSeconds = 60;
        private const int SetExecutionSeconds = 45;
        private const int MinimumAvailableMinutes = 15;
        private const int MaximumAvailableMinutes = 240;
        private const int RequiredCompoundMinimumSets = 2;
        private const int RequiredIsolationMinimumSets = 1;
        private const int OptionalAccessoryMinimumSets = 1;

        public static AdaptiveTrainingTimeProposal Adapt(
            PlannedAdaptiveTrainingSession session,
            int availableMinutes)
        {
            ArgumentNullException.ThrowIfNull(session);

            Validate(session, availableMinutes);

            var orderedExercises = session.Exercises
                .OrderBy(exercise => exercise.Order)
                .ThenBy(
                    exercise => exercise.ExerciseCode,
                    StringComparer.Ordinal)
                .ThenBy(exercise => exercise.ExerciseId)
                .ToArray();
            var originalExercises = orderedExercises
                .Select(exercise => Classify(
                    exercise,
                    orderedExercises))
                .ToArray();
            var workingExercises = originalExercises
                .Select(exercise => new WorkingExercise(exercise))
                .ToList();
            var originalDurationSeconds = CalculateDurationSeconds(
                originalExercises);
            var availableSeconds = availableMinutes * 60;

            if (originalDurationSeconds <= availableSeconds)
            {
                var originalSession = new AdaptiveTrainingSessionSnapshot(
                    originalDurationSeconds,
                    originalExercises);

                return new AdaptiveTrainingTimeProposal(
                    PolicyVersion,
                    session.RoutineDayId,
                    availableMinutes,
                    AdaptiveTrainingTimeFeasibility.Feasible,
                    originalSession,
                    originalSession,
                    [],
                    []);
            }

            RemoveLowerPriorityOptionalAccessories(
                workingExercises,
                availableSeconds);
            ReduceSetsUntilWithinDuration(
                workingExercises,
                availableSeconds,
                AdaptiveTrainingExercisePriority.OptionalAccessory);
            RemoveLastOptionalAccessoryIfNeeded(
                workingExercises,
                availableSeconds);
            ReduceSetsUntilWithinDuration(
                workingExercises,
                availableSeconds,
                AdaptiveTrainingExercisePriority.RequiredIsolation);
            ReduceSetsUntilWithinDuration(
                workingExercises,
                availableSeconds,
                AdaptiveTrainingExercisePriority.RequiredCompound);

            var adaptedExercises = workingExercises
                .OrderBy(exercise => exercise.Original.Order)
                .ThenBy(
                    exercise => exercise.Original.ExerciseCode,
                    StringComparer.Ordinal)
                .ThenBy(exercise => exercise.Original.ExerciseId)
                .Select(exercise => exercise.ToSnapshot())
                .ToArray();
            var adaptedDurationSeconds = CalculateDurationSeconds(
                adaptedExercises);
            var changes = BuildChanges(
                originalExercises,
                adaptedExercises);
            var diagnostics = BuildDiagnostics(
                changes,
                adaptedDurationSeconds > availableSeconds);
            var feasibility = adaptedDurationSeconds > availableSeconds
                ? AdaptiveTrainingTimeFeasibility.NotFeasible
                : AdaptiveTrainingTimeFeasibility.FeasibleWithChanges;

            return new AdaptiveTrainingTimeProposal(
                PolicyVersion,
                session.RoutineDayId,
                availableMinutes,
                feasibility,
                new AdaptiveTrainingSessionSnapshot(
                    originalDurationSeconds,
                    originalExercises),
                new AdaptiveTrainingSessionSnapshot(
                    adaptedDurationSeconds,
                    adaptedExercises),
                changes,
                diagnostics);
        }

        private static AdaptiveTrainingExerciseSnapshot Classify(
            PlannedAdaptiveTrainingExercise exercise,
            IReadOnlyCollection<PlannedAdaptiveTrainingExercise>
                sessionExercises)
        {
            var priority = DeterminePriority(
                exercise,
                sessionExercises);

            return new AdaptiveTrainingExerciseSnapshot(
                exercise.SourceRoutineExerciseId,
                exercise.ExerciseId,
                exercise.ExerciseCode,
                exercise.Name,
                exercise.Order,
                priority,
                exercise.Prescription);
        }

        private static AdaptiveTrainingExercisePriority DeterminePriority(
            PlannedAdaptiveTrainingExercise exercise,
            IReadOnlyCollection<PlannedAdaptiveTrainingExercise>
                sessionExercises)
        {
            if (!HasCompleteClassificationMetadata(exercise))
            {
                return AdaptiveTrainingExercisePriority
                    .UnclassifiedRequired;
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
            PlannedAdaptiveTrainingExercise exercise)
        {
            return exercise.MovementPattern.HasValue &&
                Enum.IsDefined(exercise.MovementPattern.Value) &&
                exercise.ExerciseType.HasValue &&
                Enum.IsDefined(exercise.ExerciseType.Value) &&
                !string.IsNullOrWhiteSpace(exercise.PrimaryMuscleGroup);
        }

        private static void RemoveLowerPriorityOptionalAccessories(
            List<WorkingExercise> exercises,
            int availableSeconds)
        {
            while (ExceedsDuration(exercises, availableSeconds))
            {
                var optionalAccessories = GetByDescendingPriority(
                    exercises,
                    AdaptiveTrainingExercisePriority.OptionalAccessory);

                if (optionalAccessories.Length <= 1)
                {
                    return;
                }

                exercises.Remove(optionalAccessories[0]);
            }
        }

        private static void RemoveLastOptionalAccessoryIfNeeded(
            List<WorkingExercise> exercises,
            int availableSeconds)
        {
            if (!ExceedsDuration(exercises, availableSeconds) ||
                exercises.Count <= 1)
            {
                return;
            }

            var optionalAccessory = GetByDescendingPriority(
                    exercises,
                    AdaptiveTrainingExercisePriority.OptionalAccessory)
                .FirstOrDefault();

            if (optionalAccessory is not null)
            {
                exercises.Remove(optionalAccessory);
            }
        }

        private static void ReduceSetsUntilWithinDuration(
            IReadOnlyCollection<WorkingExercise> exercises,
            int availableSeconds,
            AdaptiveTrainingExercisePriority priority)
        {
            while (ExceedsDuration(exercises, availableSeconds))
            {
                var reducibleExercises = GetByDescendingPriority(
                        exercises,
                        priority)
                    .Where(exercise =>
                        exercise.Sets > exercise.EffectiveMinimumSets)
                    .ToArray();

                if (reducibleExercises.Length == 0)
                {
                    return;
                }

                foreach (var exercise in reducibleExercises)
                {
                    if (!ExceedsDuration(exercises, availableSeconds))
                    {
                        return;
                    }

                    exercise.ReduceOneSet();
                }
            }
        }

        private static WorkingExercise[] GetByDescendingPriority(
            IEnumerable<WorkingExercise> exercises,
            AdaptiveTrainingExercisePriority priority)
        {
            return exercises
                .Where(exercise => exercise.Original.Priority == priority)
                .OrderByDescending(exercise => exercise.Original.Order)
                .ThenByDescending(
                    exercise => exercise.Original.ExerciseCode,
                    StringComparer.Ordinal)
                .ThenByDescending(exercise => exercise.Original.ExerciseId)
                .ToArray();
        }

        private static bool ExceedsDuration(
            IReadOnlyCollection<WorkingExercise> exercises,
            int availableSeconds)
        {
            return CalculateDurationSeconds(exercises) > availableSeconds;
        }

        private static int CalculateDurationSeconds(
            IReadOnlyCollection<AdaptiveTrainingExerciseSnapshot> exercises)
        {
            return CalculateDurationSeconds(
                exercises.Select(exercise => (
                    exercise.Prescription.Sets,
                    exercise.Prescription.RestSeconds)));
        }

        private static int CalculateDurationSeconds(
            IReadOnlyCollection<WorkingExercise> exercises)
        {
            return CalculateDurationSeconds(
                exercises.Select(exercise => (
                    exercise.Sets,
                    exercise.Original.Prescription.RestSeconds)));
        }

        private static int CalculateDurationSeconds(
            IEnumerable<(int Sets, int RestSeconds)> prescriptions)
        {
            var values = prescriptions.ToArray();
            var transitionCount = Math.Max(0, values.Length - 1);

            return checked(
                PreparationSeconds +
                transitionCount * TransitionSeconds +
                values.Sum(value =>
                    SetExecutionSeconds * value.Sets +
                    value.RestSeconds * (value.Sets - 1)));
        }

        private static AdaptiveTrainingTimeChange[] BuildChanges(
            IReadOnlyCollection<AdaptiveTrainingExerciseSnapshot>
                originalExercises,
            IReadOnlyCollection<AdaptiveTrainingExerciseSnapshot>
                adaptedExercises)
        {
            var adaptedBySourceId = adaptedExercises.ToDictionary(
                exercise => exercise.SourceRoutineExerciseId);
            var changes = new List<AdaptiveTrainingTimeChange>();

            foreach (var original in originalExercises
                .OrderBy(exercise => exercise.Order)
                .ThenBy(
                    exercise => exercise.ExerciseCode,
                    StringComparer.Ordinal)
                .ThenBy(exercise => exercise.ExerciseId))
            {
                if (!adaptedBySourceId.TryGetValue(
                    original.SourceRoutineExerciseId,
                    out var adapted))
                {
                    changes.Add(new AdaptiveTrainingTimeChange(
                        AdaptiveTrainingTimeChangeType
                            .OptionalAccessoryRemoved,
                        original.SourceRoutineExerciseId,
                        original.ExerciseId,
                        original.ExerciseCode,
                        original.Prescription.Sets,
                        AdaptedSets: 0));
                    continue;
                }

                if (original.Prescription.Sets !=
                    adapted.Prescription.Sets)
                {
                    changes.Add(new AdaptiveTrainingTimeChange(
                        AdaptiveTrainingTimeChangeType.ExerciseSetsReduced,
                        original.SourceRoutineExerciseId,
                        original.ExerciseId,
                        original.ExerciseCode,
                        original.Prescription.Sets,
                        adapted.Prescription.Sets));
                }
            }

            return changes.ToArray();
        }

        private static AdaptiveTrainingTimeDiagnostic[] BuildDiagnostics(
            IReadOnlyCollection<AdaptiveTrainingTimeChange> changes,
            bool minimumDurationExceeded)
        {
            var diagnostics = changes
                .Select(change => new AdaptiveTrainingTimeDiagnostic(
                    change.Type == AdaptiveTrainingTimeChangeType
                        .OptionalAccessoryRemoved
                        ? AdaptiveTrainingTimeDiagnosticCode
                            .OptionalAccessoryRemovedForTime
                        : AdaptiveTrainingTimeDiagnosticCode
                            .ExerciseSetsReducedForTime,
                    change.SourceRoutineExerciseId,
                    change.ExerciseId,
                    change.ExerciseCode))
                .ToList();

            if (minimumDurationExceeded)
            {
                diagnostics.Add(new AdaptiveTrainingTimeDiagnostic(
                    AdaptiveTrainingTimeDiagnosticCode
                        .MinimumSessionExceedsAvailableTime));
            }

            return diagnostics.ToArray();
        }

        private static void Validate(
            PlannedAdaptiveTrainingSession session,
            int availableMinutes)
        {
            if (availableMinutes is < MinimumAvailableMinutes or
                > MaximumAvailableMinutes)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(availableMinutes));
            }

            ArgumentNullException.ThrowIfNull(session.Exercises);

            if (session.Exercises.Count == 0)
            {
                throw new ArgumentException(
                    "La sesión planificada debe contener ejercicios.",
                    nameof(session));
            }

            if (session.Exercises.Any(exercise => exercise is null))
            {
                throw new ArgumentException(
                    "La sesión contiene un ejercicio nulo.",
                    nameof(session));
            }

            if (session.Exercises.Any(exercise =>
                exercise.SourceRoutineExerciseId == Guid.Empty ||
                exercise.ExerciseId == Guid.Empty ||
                exercise.Order <= 0 ||
                string.IsNullOrWhiteSpace(exercise.Name)))
            {
                throw new ArgumentException(
                    "La identidad y el orden de cada ejercicio son " +
                        "obligatorios.",
                    nameof(session));
            }

            if (session.Exercises
                .GroupBy(exercise => exercise.Order)
                .Any(group => group.Count() > 1))
            {
                throw new ArgumentException(
                    "Los órdenes de ejercicios deben ser únicos.",
                    nameof(session));
            }

            if (session.Exercises
                .GroupBy(exercise => exercise.SourceRoutineExerciseId)
                .Any(group => group.Count() > 1))
            {
                throw new ArgumentException(
                    "Los ejercicios fuente deben ser únicos.",
                    nameof(session));
            }

            foreach (var exercise in session.Exercises)
            {
                ValidatePrescription(exercise.Prescription, session);
            }
        }

        private static void ValidatePrescription(
            AdaptiveTrainingPrescription prescription,
            PlannedAdaptiveTrainingSession session)
        {
            ArgumentNullException.ThrowIfNull(prescription);

            if (prescription.Sets is < 1 or > 20 ||
                prescription.MinReps is < 1 or > 100 ||
                prescription.MaxReps is < 1 or > 100 ||
                prescription.MinReps > prescription.MaxReps ||
                prescription.MinRir is < 0 or > 5 ||
                prescription.MaxRir is < 0 or > 5 ||
                prescription.MinRir > prescription.MaxRir ||
                prescription.RestSeconds is < 15 or > 900)
            {
                throw new ArgumentException(
                    "La prescripción planificada no es válida.",
                    nameof(session));
            }
        }

        private sealed class WorkingExercise
        {
            public AdaptiveTrainingExerciseSnapshot Original { get; }

            public int Sets { get; private set; }

            public int EffectiveMinimumSets { get; }

            public WorkingExercise(
                AdaptiveTrainingExerciseSnapshot original)
            {
                Original = original;
                Sets = original.Prescription.Sets;
                EffectiveMinimumSets = GetEffectiveMinimumSets(original);
            }

            public void ReduceOneSet()
            {
                if (Sets <= EffectiveMinimumSets)
                {
                    throw new InvalidOperationException(
                        "No se puede reducir debajo del piso efectivo.");
                }

                Sets--;
            }

            public AdaptiveTrainingExerciseSnapshot ToSnapshot()
            {
                return Original with
                {
                    Prescription = Original.Prescription with
                    {
                        Sets = Sets
                    }
                };
            }

            private static int GetEffectiveMinimumSets(
                AdaptiveTrainingExerciseSnapshot exercise)
            {
                var policyMinimum = exercise.Priority switch
                {
                    AdaptiveTrainingExercisePriority.RequiredCompound =>
                        RequiredCompoundMinimumSets,
                    AdaptiveTrainingExercisePriority.RequiredIsolation =>
                        RequiredIsolationMinimumSets,
                    AdaptiveTrainingExercisePriority.OptionalAccessory =>
                        OptionalAccessoryMinimumSets,
                    AdaptiveTrainingExercisePriority.UnclassifiedRequired =>
                        exercise.Prescription.Sets,
                    _ => throw new ArgumentOutOfRangeException(
                        nameof(exercise))
                };

                return Math.Min(exercise.Prescription.Sets, policyMinimum);
            }
        }
    }
}
