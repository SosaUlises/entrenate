using Entrenate.Application.Coach.AdaptiveTraining.TimeAdaptation;
using Entrenate.Domain.Enums;

namespace Entrenate.Application.Coach.AdaptiveTraining.Substitution
{
    public static class AdaptiveTrainingSubstitutionEngine
    {
        public const string PolicyVersion = "3.0-at2";

        public static AdaptiveTrainingSubstitutionProposal Adapt(
            AdaptiveTrainingSubstitutionRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            return Adapt(
                request.Session,
                request.Catalog,
                request.AvailableEquipmentIds,
                request.ExcludedExerciseIds);
        }

        public static AdaptiveTrainingSubstitutionProposal Adapt(
            PlannedSubstitutionSession session,
            IReadOnlyCollection<AdaptiveSubstitutionCandidate> catalog,
            IReadOnlyCollection<Guid> availableEquipmentIds,
            IReadOnlyCollection<Guid>? excludedExerciseIds = null)
        {
            ArgumentNullException.ThrowIfNull(session);
            ArgumentNullException.ThrowIfNull(catalog);
            ArgumentNullException.ThrowIfNull(availableEquipmentIds);

            Validate(session);

            var excludedSet = excludedExerciseIds is not null
                ? excludedExerciseIds.Where(id => id != Guid.Empty).ToHashSet()
                : [];
            var availableEquipmentSet = availableEquipmentIds
                .Where(id => id != Guid.Empty)
                .ToHashSet();

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

            var originalSession = new AdaptiveTrainingSubstitutionSessionSnapshot(
                session.RoutineDayId,
                originalSnapshots);

            var originalSessionExerciseIds = orderedExercises
                .Select(exercise => exercise.ExerciseId)
                .ToHashSet();

            var usedExerciseIds = orderedExercises
                .Where(exercise => IsRealizable(
                    exercise.ExerciseId,
                    exercise.RequiredEquipmentIds,
                    availableEquipmentSet,
                    excludedSet))
                .Select(exercise => exercise.ExerciseId)
                .ToHashSet();

            var adaptedExercises = new List<AdaptiveTrainingSubstitutionExerciseSnapshot>();
            var changes = new List<AdaptiveTrainingSubstitutionChange>();
            var diagnostics = new List<AdaptiveTrainingSubstitutionDiagnostic>();
            var hasMissingRequiredSubstitute = false;

            foreach (var original in originalSnapshots)
            {
                var isExcluded = excludedSet.Contains(original.ExerciseId);
                var hasMissingEquipment = original.RequiredEquipmentIds is not null &&
                    original.RequiredEquipmentIds.Any(equipmentId =>
                        !availableEquipmentSet.Contains(equipmentId));

                if (!isExcluded && !hasMissingEquipment)
                {
                    adaptedExercises.Add(original);
                    continue;
                }

                var reason = DetermineReason(isExcluded, hasMissingEquipment);

                AdaptiveSubstitutionCandidate? bestCandidate = null;

                if (original.Priority != AdaptiveTrainingExercisePriority.UnclassifiedRequired)
                {
                    bestCandidate = catalog
                        .Where(candidate => IsCandidateEligible(
                            candidate,
                            original,
                            availableEquipmentSet,
                            excludedSet,
                            usedExerciseIds))
                        .Select(candidate => new
                        {
                            Candidate = candidate,
                            SharedSecondaryCount = CalculateSharedSecondaryMuscles(
                                original.SecondaryMuscleGroups,
                                candidate.SecondaryMuscleGroups),
                            HasSessionConflict = originalSessionExerciseIds.Contains(
                                candidate.ExerciseId) ? 1 : 0
                        })
                        .OrderByDescending(x => x.SharedSecondaryCount)
                        .ThenBy(x => x.HasSessionConflict)
                        .ThenBy(
                            x => x.Candidate.Code,
                            StringComparer.Ordinal)
                        .ThenBy(x => x.Candidate.ExerciseId)
                        .Select(x => x.Candidate)
                        .FirstOrDefault();
                }

                if (bestCandidate is not null)
                {
                    usedExerciseIds.Add(bestCandidate.ExerciseId);

                    var adaptedSnapshot = new AdaptiveTrainingSubstitutionExerciseSnapshot(
                        original.SourceRoutineExerciseId,
                        bestCandidate.ExerciseId,
                        bestCandidate.Code,
                        bestCandidate.Name,
                        original.Order,
                        original.Priority,
                        bestCandidate.MovementPattern,
                        bestCandidate.ExerciseType,
                        bestCandidate.PrimaryMuscleGroup,
                        NormalizeSecondaryMuscleGroups(
                            bestCandidate.SecondaryMuscleGroups),
                        bestCandidate.RequiredEquipmentIds?.ToArray() ?? [],
                        original.Prescription);

                    adaptedExercises.Add(adaptedSnapshot);

                    changes.Add(new AdaptiveTrainingSubstitutionChange(
                        AdaptiveTrainingSubstitutionChangeType.Substituted,
                        reason,
                        original,
                        adaptedSnapshot));

                    diagnostics.Add(new AdaptiveTrainingSubstitutionDiagnostic(
                        AdaptiveTrainingSubstitutionDiagnosticCode.ExerciseSubstituted,
                        original.SourceRoutineExerciseId,
                        original.ExerciseId,
                        original.ExerciseCode,
                        bestCandidate.ExerciseId,
                        bestCandidate.Code));
                }
                else
                {
                    if (original.Priority == AdaptiveTrainingExercisePriority.OptionalAccessory)
                    {
                        changes.Add(new AdaptiveTrainingSubstitutionChange(
                            AdaptiveTrainingSubstitutionChangeType.OptionalRemoved,
                            reason,
                            original,
                            null));

                        diagnostics.Add(new AdaptiveTrainingSubstitutionDiagnostic(
                            AdaptiveTrainingSubstitutionDiagnosticCode
                                .OptionalExerciseRemovedNoSubstitute,
                            original.SourceRoutineExerciseId,
                            original.ExerciseId,
                            original.ExerciseCode));
                    }
                    else
                    {
                        hasMissingRequiredSubstitute = true;
                        adaptedExercises.Add(original);

                        changes.Add(new AdaptiveTrainingSubstitutionChange(
                            AdaptiveTrainingSubstitutionChangeType.RequiredUnavailable,
                            reason,
                            original,
                            original));

                        diagnostics.Add(new AdaptiveTrainingSubstitutionDiagnostic(
                            AdaptiveTrainingSubstitutionDiagnosticCode
                                .RequiredExerciseHasNoSubstitute,
                            original.SourceRoutineExerciseId,
                            original.ExerciseId,
                            original.ExerciseCode));
                    }
                }
            }

            var feasibility = hasMissingRequiredSubstitute
                ? AdaptiveTrainingSubstitutionFeasibility.NotFeasible
                : changes.Count > 0
                    ? AdaptiveTrainingSubstitutionFeasibility.FeasibleWithChanges
                    : AdaptiveTrainingSubstitutionFeasibility.Feasible;

            var adaptedSession = new AdaptiveTrainingSubstitutionSessionSnapshot(
                session.RoutineDayId,
                adaptedExercises);

            return new AdaptiveTrainingSubstitutionProposal(
                PolicyVersion,
                session.RoutineDayId,
                feasibility,
                originalSession,
                adaptedSession,
                changes,
                diagnostics);
        }

        private static bool IsRealizable(
            Guid exerciseId,
            IReadOnlyCollection<Guid>? requiredEquipmentIds,
            IReadOnlySet<Guid> availableEquipment,
            IReadOnlySet<Guid> excludedExercises)
        {
            if (excludedExercises.Contains(exerciseId))
            {
                return false;
            }

            if (requiredEquipmentIds is null || requiredEquipmentIds.Count == 0)
            {
                return true;
            }

            return requiredEquipmentIds.All(equipmentId =>
                equipmentId != Guid.Empty &&
                availableEquipment.Contains(equipmentId));
        }

        private static AdaptiveSubstitutionReason DetermineReason(
            bool isExcluded,
            bool hasMissingEquipment)
        {
            if (isExcluded && hasMissingEquipment)
            {
                return AdaptiveSubstitutionReason.EquipmentUnavailable |
                    AdaptiveSubstitutionReason.UserExcludedExercise;
            }

            if (isExcluded)
            {
                return AdaptiveSubstitutionReason.UserExcludedExercise;
            }

            return AdaptiveSubstitutionReason.EquipmentUnavailable;
        }

        private static bool IsCandidateEligible(
            AdaptiveSubstitutionCandidate candidate,
            AdaptiveTrainingSubstitutionExerciseSnapshot original,
            IReadOnlySet<Guid> availableEquipment,
            IReadOnlySet<Guid> excludedExercises,
            IReadOnlySet<Guid> usedExerciseIds)
        {
            if (!candidate.IsActive ||
                candidate.ExerciseId == Guid.Empty ||
                string.IsNullOrWhiteSpace(candidate.Code) ||
                string.IsNullOrWhiteSpace(candidate.Name) ||
                string.IsNullOrWhiteSpace(candidate.PrimaryMuscleGroup) ||
                !candidate.MovementPattern.HasValue ||
                !Enum.IsDefined(candidate.MovementPattern.Value) ||
                !candidate.ExerciseType.HasValue ||
                !Enum.IsDefined(candidate.ExerciseType.Value) ||
                candidate.SecondaryMuscleGroups is null ||
                candidate.RequiredEquipmentIds is null)
            {
                return false;
            }

            if (candidate.ExerciseId == original.ExerciseId)
            {
                return false;
            }

            if (excludedExercises.Contains(candidate.ExerciseId))
            {
                return false;
            }

            if (usedExerciseIds.Contains(candidate.ExerciseId))
            {
                return false;
            }

            if (!candidate.RequiredEquipmentIds.All(equipmentId =>
                equipmentId != Guid.Empty &&
                availableEquipment.Contains(equipmentId)))
            {
                return false;
            }

            if (candidate.MovementPattern.Value != original.MovementPattern!.Value)
            {
                return false;
            }

            if (!string.Equals(
                candidate.PrimaryMuscleGroup.Trim(),
                original.PrimaryMuscleGroup.Trim(),
                StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (candidate.ExerciseType.Value != original.ExerciseType!.Value)
            {
                return false;
            }

            return true;
        }

        private static int CalculateSharedSecondaryMuscles(
            IReadOnlyCollection<string>? originalSecondary,
            IReadOnlyCollection<string>? candidateSecondary)
        {
            if (originalSecondary is null || candidateSecondary is null ||
                originalSecondary.Count == 0 || candidateSecondary.Count == 0)
            {
                return 0;
            }

            var originalSet = originalSecondary
                .Where(group => !string.IsNullOrWhiteSpace(group))
                .Select(group => group.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return candidateSecondary
                .Where(group => !string.IsNullOrWhiteSpace(group))
                .Select(group => group.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count(group => originalSet.Contains(group));
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

        private static AdaptiveTrainingExercisePriority DeterminePriority(
            PlannedSubstitutionExercise exercise,
            IReadOnlyCollection<PlannedSubstitutionExercise> sessionExercises)
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
            PlannedSubstitutionExercise exercise)
        {
            return exercise.MovementPattern.HasValue &&
                Enum.IsDefined(exercise.MovementPattern.Value) &&
                exercise.ExerciseType.HasValue &&
                Enum.IsDefined(exercise.ExerciseType.Value) &&
                !string.IsNullOrWhiteSpace(exercise.PrimaryMuscleGroup);
        }

        private static AdaptiveTrainingSubstitutionExerciseSnapshot ToSnapshot(
            PlannedSubstitutionExercise exercise,
            AdaptiveTrainingExercisePriority priority)
        {
            return new AdaptiveTrainingSubstitutionExerciseSnapshot(
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

        private static void Validate(PlannedSubstitutionSession session)
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
            }
        }
    }
}

