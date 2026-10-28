using Entrenate.Application.Coach.AdaptiveTraining.Lighter;
using Entrenate.Application.Coach.AdaptiveTraining.Substitution;
using Entrenate.Application.Coach.AdaptiveTraining.TimeAdaptation;
using Entrenate.Domain.Enums;

namespace Entrenate.Application.Coach.AdaptiveTraining.Proposal
{
    public static class SessionAdaptationProposalEngine
    {
        public const string PolicyVersion = "3.0-at4";

        private const int PreparationSeconds = 5 * 60;
        private const int TransitionSeconds = 60;
        private const int SetExecutionSeconds = 45;
        private const int MinimumAvailableMinutes = 15;
        private const int MaximumAvailableMinutes = 240;

        public static SessionAdaptationProposal Adapt(
            PlannedAdaptiveSession originalSession,
            SessionAdaptationRequest request,
            IReadOnlyCollection<AdaptiveSubstitutionCandidate>? catalog = null)
        {
            ArgumentNullException.ThrowIfNull(originalSession);
            ArgumentNullException.ThrowIfNull(request);

            Validate(originalSession, request);

            var orderedOriginal = originalSession.Exercises
                .OrderBy(e => e.Order)
                .ThenBy(e => e.ExerciseCode, StringComparer.Ordinal)
                .ThenBy(e => e.ExerciseId)
                .ToArray();

            var originalSnapshots = orderedOriginal
                .Select(e => ToSessionExerciseSnapshot(
                    e,
                    DeterminePriority(e, orderedOriginal)))
                .ToArray();

            var originalDurationSeconds = CalculateDurationSeconds(
                originalSnapshots);

            var originalSessionSnapshot = new SessionPlanSnapshot(
                originalDurationSeconds,
                originalSnapshots);

            var workingExercises = originalSnapshots
                .Select(e => new WorkingExercise(e))
                .ToList();

            var consolidatedChanges = new List<SessionAdaptationChange>();
            var consolidatedDiagnostics = new List<SessionAdaptationDiagnostic>();
            var isStageNotFeasible = false;

            // Stage 1: Substitutions (AT-2)
            var hasEquipmentFilter = request.AvailableEquipmentIds is not null;
            var hasExcludedExercises = request.ExcludedExerciseIds is not null &&
                request.ExcludedExerciseIds.Count > 0;

            if (hasEquipmentFilter || hasExcludedExercises)
            {
                var plannedSubExercises = workingExercises
                    .Select(w => new PlannedSubstitutionExercise(
                        w.SourceRoutineExerciseId,
                        w.ExerciseId,
                        w.ExerciseCode,
                        w.Name,
                        w.Order,
                        w.MovementPattern,
                        w.ExerciseType,
                        w.PrimaryMuscleGroup,
                        w.SecondaryMuscleGroups,
                        w.RequiredEquipmentIds,
                        w.Priority == AdaptiveTrainingExercisePriority.OptionalAccessory
                            ? false
                            : w.Priority == AdaptiveTrainingExercisePriority.RequiredIsolation
                                ? true
                                : (bool?)null,
                        w.Prescription))
                    .ToArray();

                var plannedSubSession = new PlannedSubstitutionSession(
                    originalSession.RoutineDayId,
                    plannedSubExercises);

                var subProposal = AdaptiveTrainingSubstitutionEngine.Adapt(
                    plannedSubSession,
                    catalog ?? [],
                    request.AvailableEquipmentIds ?? [],
                    request.ExcludedExerciseIds);

                if (subProposal.Feasibility == AdaptiveTrainingSubstitutionFeasibility.NotFeasible)
                {
                    isStageNotFeasible = true;
                }

                foreach (var change in subProposal.Changes)
                {
                    switch (change.Type)
                    {
                        case AdaptiveTrainingSubstitutionChangeType.Substituted:
                            consolidatedChanges.Add(new SessionAdaptationChange(
                                SessionAdaptationChangeType.ExerciseSubstituted,
                                change.OriginalExercise.SourceRoutineExerciseId,
                                change.OriginalExercise.ExerciseId,
                                change.OriginalExercise.ExerciseCode,
                                SubstitutedExerciseId: change.AdaptedExercise?.ExerciseId,
                                SubstitutedExerciseCode: change.AdaptedExercise?.ExerciseCode,
                                SubstitutionReason: change.Reason));
                            break;

                        case AdaptiveTrainingSubstitutionChangeType.OptionalRemoved:
                            consolidatedChanges.Add(new SessionAdaptationChange(
                                SessionAdaptationChangeType.OptionalAccessoryRemovedNoSubstitute,
                                change.OriginalExercise.SourceRoutineExerciseId,
                                change.OriginalExercise.ExerciseId,
                                change.OriginalExercise.ExerciseCode,
                                SubstitutionReason: change.Reason));
                            break;

                        case AdaptiveTrainingSubstitutionChangeType.RequiredUnavailable:
                            consolidatedChanges.Add(new SessionAdaptationChange(
                                SessionAdaptationChangeType.RequiredExerciseUnavailable,
                                change.OriginalExercise.SourceRoutineExerciseId,
                                change.OriginalExercise.ExerciseId,
                                change.OriginalExercise.ExerciseCode,
                                SubstitutionReason: change.Reason));
                            break;
                    }
                }

                foreach (var diag in subProposal.Diagnostics)
                {
                    switch (diag.Code)
                    {
                        case AdaptiveTrainingSubstitutionDiagnosticCode.ExerciseSubstituted:
                            consolidatedDiagnostics.Add(new SessionAdaptationDiagnostic(
                                SessionAdaptationDiagnosticCode.ExerciseSubstituted,
                                diag.SourceRoutineExerciseId,
                                diag.ExerciseId,
                                diag.ExerciseCode,
                                diag.SubstitutedExerciseId,
                                diag.SubstitutedExerciseCode));
                            break;

                        case AdaptiveTrainingSubstitutionDiagnosticCode.OptionalExerciseRemovedNoSubstitute:
                            consolidatedDiagnostics.Add(new SessionAdaptationDiagnostic(
                                SessionAdaptationDiagnosticCode.OptionalExerciseRemovedNoSubstitute,
                                diag.SourceRoutineExerciseId,
                                diag.ExerciseId,
                                diag.ExerciseCode));
                            break;

                        case AdaptiveTrainingSubstitutionDiagnosticCode.RequiredExerciseHasNoSubstitute:
                            consolidatedDiagnostics.Add(new SessionAdaptationDiagnostic(
                                SessionAdaptationDiagnosticCode.RequiredExerciseHasNoSubstitute,
                                diag.SourceRoutineExerciseId,
                                diag.ExerciseId,
                                diag.ExerciseCode));
                            break;
                    }
                }

                // Update working exercises to match adapted session from AT-2
                var subAdaptedById = subProposal.AdaptedSession.Exercises
                    .ToDictionary(e => e.SourceRoutineExerciseId);

                workingExercises = workingExercises
                    .Where(w => subAdaptedById.ContainsKey(w.SourceRoutineExerciseId))
                    .Select(w =>
                    {
                        var adapted = subAdaptedById[w.SourceRoutineExerciseId];
                        return new WorkingExercise(
                            adapted.SourceRoutineExerciseId,
                            adapted.ExerciseId,
                            adapted.ExerciseCode,
                            adapted.Name,
                            adapted.Order,
                            adapted.Priority,
                            adapted.MovementPattern,
                            adapted.ExerciseType,
                            adapted.PrimaryMuscleGroup,
                            adapted.SecondaryMuscleGroups,
                            adapted.RequiredEquipmentIds,
                            adapted.Prescription);
                    })
                    .ToList();
            }

            // Stage 2: Time Adaptation (AT-1)
            if (request.AvailableMinutes.HasValue)
            {
                var plannedTimeExercises = workingExercises
                    .Select(w => new PlannedAdaptiveTrainingExercise(
                        w.SourceRoutineExerciseId,
                        w.ExerciseId,
                        w.ExerciseCode,
                        w.Name,
                        w.Order,
                        w.MovementPattern,
                        w.ExerciseType,
                        w.PrimaryMuscleGroup,
                        w.Priority == AdaptiveTrainingExercisePriority.OptionalAccessory
                            ? false
                            : w.Priority == AdaptiveTrainingExercisePriority.RequiredIsolation
                                ? true
                                : (bool?)null,
                        w.Prescription))
                    .ToArray();

                var plannedTimeSession = new PlannedAdaptiveTrainingSession(
                    originalSession.RoutineDayId,
                    plannedTimeExercises);

                var timeProposal = AdaptiveTrainingTimeEngine.Adapt(
                    plannedTimeSession,
                    request.AvailableMinutes.Value);

                if (timeProposal.Feasibility == AdaptiveTrainingTimeFeasibility.NotFeasible)
                {
                    isStageNotFeasible = true;
                }

                foreach (var change in timeProposal.Changes)
                {
                    switch (change.Type)
                    {
                        case AdaptiveTrainingTimeChangeType.ExerciseSetsReduced:
                            consolidatedChanges.Add(new SessionAdaptationChange(
                                SessionAdaptationChangeType.ExerciseSetsReducedForTime,
                                change.SourceRoutineExerciseId,
                                change.ExerciseId,
                                change.ExerciseCode,
                                OriginalSets: change.OriginalSets,
                                AdaptedSets: change.AdaptedSets));
                            break;

                        case AdaptiveTrainingTimeChangeType.OptionalAccessoryRemoved:
                            consolidatedChanges.Add(new SessionAdaptationChange(
                                SessionAdaptationChangeType.OptionalAccessoryRemovedForTime,
                                change.SourceRoutineExerciseId,
                                change.ExerciseId,
                                change.ExerciseCode,
                                OriginalSets: change.OriginalSets,
                                AdaptedSets: 0));
                            break;
                    }
                }

                foreach (var diag in timeProposal.Diagnostics)
                {
                    switch (diag.Code)
                    {
                        case AdaptiveTrainingTimeDiagnosticCode.ExerciseSetsReducedForTime:
                            consolidatedDiagnostics.Add(new SessionAdaptationDiagnostic(
                                SessionAdaptationDiagnosticCode.ExerciseSetsReducedForTime,
                                diag.SourceRoutineExerciseId,
                                diag.ExerciseId,
                                diag.ExerciseCode));
                            break;

                        case AdaptiveTrainingTimeDiagnosticCode.OptionalAccessoryRemovedForTime:
                            consolidatedDiagnostics.Add(new SessionAdaptationDiagnostic(
                                SessionAdaptationDiagnosticCode.OptionalAccessoryRemovedForTime,
                                diag.SourceRoutineExerciseId,
                                diag.ExerciseId,
                                diag.ExerciseCode));
                            break;

                        case AdaptiveTrainingTimeDiagnosticCode.MinimumSessionExceedsAvailableTime:
                            consolidatedDiagnostics.Add(new SessionAdaptationDiagnostic(
                                SessionAdaptationDiagnosticCode.MinimumSessionExceedsAvailableTime,
                                diag.SourceRoutineExerciseId,
                                diag.ExerciseId,
                                diag.ExerciseCode));
                            break;
                    }
                }

                // Update working exercises to match adapted session from AT-1
                var timeAdaptedById = timeProposal.AdaptedSession.Exercises
                    .ToDictionary(e => e.SourceRoutineExerciseId);

                workingExercises = workingExercises
                    .Where(w => timeAdaptedById.ContainsKey(w.SourceRoutineExerciseId))
                    .Select(w =>
                    {
                        var adapted = timeAdaptedById[w.SourceRoutineExerciseId];
                        return new WorkingExercise(
                            w.SourceRoutineExerciseId,
                            w.ExerciseId,
                            w.ExerciseCode,
                            w.Name,
                            w.Order,
                            adapted.Priority,
                            w.MovementPattern,
                            w.ExerciseType,
                            w.PrimaryMuscleGroup,
                            w.SecondaryMuscleGroups,
                            w.RequiredEquipmentIds,
                            adapted.Prescription);
                    })
                    .ToList();
            }

            // Stage 3: Lighter Mode (AT-3)
            if (request.EffortPreference == SessionEffortPreference.Lighter)
            {
                var plannedLighterExercises = workingExercises
                    .Select(w => new PlannedLighterExercise(
                        w.SourceRoutineExerciseId,
                        w.ExerciseId,
                        w.ExerciseCode,
                        w.Name,
                        w.Order,
                        w.MovementPattern,
                        w.ExerciseType,
                        w.PrimaryMuscleGroup,
                        w.SecondaryMuscleGroups,
                        w.RequiredEquipmentIds,
                        w.Priority == AdaptiveTrainingExercisePriority.OptionalAccessory
                            ? false
                            : w.Priority == AdaptiveTrainingExercisePriority.RequiredIsolation
                                ? true
                                : (bool?)null,
                        AdaptiveTrainingLighterPrescription.FromStandard(w.Prescription)))
                    .ToArray();

                var plannedLighterSession = new PlannedLighterSession(
                    originalSession.RoutineDayId,
                    plannedLighterExercises);

                var lighterProposal = AdaptiveTrainingLighterEngine.Adapt(
                    plannedLighterSession);

                foreach (var change in lighterProposal.Changes)
                {
                    switch (change.Type)
                    {
                        case AdaptiveTrainingLighterChangeType.RirIncreased:
                            consolidatedChanges.Add(new SessionAdaptationChange(
                                SessionAdaptationChangeType.RirIncreased,
                                change.SourceRoutineExerciseId,
                                change.ExerciseId,
                                change.ExerciseCode,
                                OriginalMinRir: change.OriginalMinRir,
                                OriginalMaxRir: change.OriginalMaxRir,
                                AdaptedMinRir: change.AdaptedMinRir,
                                AdaptedMaxRir: change.AdaptedMaxRir));
                            break;

                        case AdaptiveTrainingLighterChangeType.OptionalAccessorySetsReduced:
                            consolidatedChanges.Add(new SessionAdaptationChange(
                                SessionAdaptationChangeType.OptionalAccessorySetsReducedForLighter,
                                change.SourceRoutineExerciseId,
                                change.ExerciseId,
                                change.ExerciseCode,
                                OriginalSets: change.OriginalSets,
                                AdaptedSets: change.AdaptedSets));
                            break;
                    }
                }

                foreach (var diag in lighterProposal.Diagnostics)
                {
                    switch (diag.Code)
                    {
                        case AdaptiveTrainingLighterDiagnosticCode.RirIncreased:
                            consolidatedDiagnostics.Add(new SessionAdaptationDiagnostic(
                                SessionAdaptationDiagnosticCode.RirIncreased,
                                diag.SourceRoutineExerciseId,
                                diag.ExerciseId,
                                diag.ExerciseCode));
                            break;

                        case AdaptiveTrainingLighterDiagnosticCode.OptionalAccessorySetsReduced:
                            consolidatedDiagnostics.Add(new SessionAdaptationDiagnostic(
                                SessionAdaptationDiagnosticCode.OptionalAccessorySetsReducedForLighter,
                                diag.SourceRoutineExerciseId,
                                diag.ExerciseId,
                                diag.ExerciseCode));
                            break;

                        case AdaptiveTrainingLighterDiagnosticCode.NeutralLighterTransformation:
                            consolidatedDiagnostics.Add(new SessionAdaptationDiagnostic(
                                SessionAdaptationDiagnosticCode.NeutralLighterTransformation));
                            break;
                    }
                }

                // Update working exercises with lighter prescription
                var lighterAdaptedById = lighterProposal.AdaptedSession.Exercises
                    .ToDictionary(e => e.SourceRoutineExerciseId);

                workingExercises = workingExercises
                    .Where(w => lighterAdaptedById.ContainsKey(w.SourceRoutineExerciseId))
                    .Select(w =>
                    {
                        var adapted = lighterAdaptedById[w.SourceRoutineExerciseId];
                        return new WorkingExercise(
                            w.SourceRoutineExerciseId,
                            w.ExerciseId,
                            w.ExerciseCode,
                            w.Name,
                            w.Order,
                            w.Priority,
                            w.MovementPattern,
                            w.ExerciseType,
                            w.PrimaryMuscleGroup,
                            w.SecondaryMuscleGroups,
                            w.RequiredEquipmentIds,
                            new AdaptiveTrainingPrescription(
                                adapted.Prescription.Sets,
                                adapted.Prescription.MinReps,
                                adapted.Prescription.MaxReps,
                                adapted.Prescription.MinRir ?? w.Prescription.MinRir,
                                adapted.Prescription.MaxRir ?? w.Prescription.MaxRir,
                                adapted.Prescription.RestSeconds,
                                adapted.Prescription.Notes));
                    })
                    .ToList();
            }

            var adaptedSnapshots = workingExercises
                .OrderBy(w => w.Order)
                .ThenBy(w => w.ExerciseCode, StringComparer.Ordinal)
                .ThenBy(w => w.ExerciseId)
                .Select(w => new SessionExerciseSnapshot(
                    w.SourceRoutineExerciseId,
                    w.ExerciseId,
                    w.ExerciseCode,
                    w.Name,
                    w.Order,
                    w.Priority,
                    w.MovementPattern,
                    w.ExerciseType,
                    w.PrimaryMuscleGroup,
                    w.SecondaryMuscleGroups,
                    w.RequiredEquipmentIds,
                    w.Prescription))
                .ToArray();

            var adaptedDurationSeconds = CalculateDurationSeconds(
                adaptedSnapshots);

            var adaptedSessionSnapshot = new SessionPlanSnapshot(
                adaptedDurationSeconds,
                adaptedSnapshots);

            var feasibility = isStageNotFeasible
                ? SessionAdaptationFeasibility.NotFeasible
                : consolidatedChanges.Count > 0
                    ? SessionAdaptationFeasibility.FeasibleWithChanges
                    : SessionAdaptationFeasibility.Feasible;

            var (proposalHash, proposalId) = SessionAdaptationProposalCanonicalizer.Calculate(
                PolicyVersion,
                originalSession,
                request,
                catalog,
                feasibility,
                adaptedSessionSnapshot,
                consolidatedChanges,
                consolidatedDiagnostics);

            var source = new SessionAdaptationSource(
                originalSession.RoutineId,
                originalSession.RoutineDayId,
                originalSession.RoutineUpdatedAtUtc);

            var context = new SessionAdaptationContext(
                request.AvailableMinutes,
                request.AvailableEquipmentIds?.ToArray() ?? [],
                request.ExcludedExerciseIds?.ToArray() ?? [],
                request.EffortPreference);

            return new SessionAdaptationProposal(
                proposalId,
                PolicyVersion,
                proposalHash,
                feasibility,
                source,
                context,
                originalSessionSnapshot,
                adaptedSessionSnapshot,
                consolidatedChanges,
                consolidatedDiagnostics);
        }

        private static int CalculateDurationSeconds(
            IReadOnlyCollection<SessionExerciseSnapshot> exercises)
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
            PlannedAdaptiveExercise exercise,
            IReadOnlyCollection<PlannedAdaptiveExercise> sessionExercises)
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
            PlannedAdaptiveExercise exercise)
        {
            return exercise.MovementPattern.HasValue &&
                Enum.IsDefined(exercise.MovementPattern.Value) &&
                exercise.ExerciseType.HasValue &&
                Enum.IsDefined(exercise.ExerciseType.Value) &&
                !string.IsNullOrWhiteSpace(exercise.PrimaryMuscleGroup);
        }

        private static SessionExerciseSnapshot ToSessionExerciseSnapshot(
            PlannedAdaptiveExercise exercise,
            AdaptiveTrainingExercisePriority priority)
        {
            return new SessionExerciseSnapshot(
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

        private static void Validate(
            PlannedAdaptiveSession session,
            SessionAdaptationRequest request)
        {
            if (session.RoutineDayId == Guid.Empty)
            {
                throw new ArgumentException(
                    "RoutineDayId must be provided in session.",
                    nameof(session));
            }

            if (request.RoutineDayId == Guid.Empty)
            {
                throw new ArgumentException(
                    "RoutineDayId must be provided in request.",
                    nameof(request));
            }

            if (session.RoutineDayId != request.RoutineDayId)
            {
                throw new ArgumentException(
                    $"Session RoutineDayId ({session.RoutineDayId}) does not match request RoutineDayId ({request.RoutineDayId}).");
            }

            if (session.Exercises is null || session.Exercises.Count == 0)
            {
                throw new ArgumentException(
                    "A session must contain at least one exercise.",
                    nameof(session));
            }

            if (request.AvailableMinutes.HasValue &&
                (request.AvailableMinutes.Value < MinimumAvailableMinutes ||
                 request.AvailableMinutes.Value > MaximumAvailableMinutes))
            {
                throw new ArgumentException(
                    $"AvailableMinutes must be between {MinimumAvailableMinutes} and {MaximumAvailableMinutes}.",
                    nameof(request));
            }

            if (!Enum.IsDefined(request.EffortPreference))
            {
                throw new ArgumentException(
                    "Invalid EffortPreference.",
                    nameof(request));
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
                        $"MinReps cannot exceed MaxReps for exercise {exercise.ExerciseId}.",
                        nameof(session));
                }

                if (exercise.Prescription.MinRir is < 0 or > 5 ||
                    exercise.Prescription.MaxRir is < 0 or > 5 ||
                    exercise.Prescription.MinRir > exercise.Prescription.MaxRir)
                {
                    throw new ArgumentException(
                        $"Invalid RIR range [{exercise.Prescription.MinRir}..{exercise.Prescription.MaxRir}] for exercise {exercise.ExerciseId}.",
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

        private sealed class WorkingExercise
        {
            public Guid SourceRoutineExerciseId { get; }
            public Guid ExerciseId { get; set; }
            public string? ExerciseCode { get; set; }
            public string Name { get; set; }
            public int Order { get; }
            public AdaptiveTrainingExercisePriority Priority { get; set; }
            public PatronMovimiento? MovementPattern { get; set; }
            public TipoEjercicio? ExerciseType { get; set; }
            public string PrimaryMuscleGroup { get; set; }
            public IReadOnlyCollection<string> SecondaryMuscleGroups { get; set; }
            public IReadOnlyCollection<Guid> RequiredEquipmentIds { get; set; }
            public AdaptiveTrainingPrescription Prescription { get; set; }

            public WorkingExercise(SessionExerciseSnapshot snapshot)
                : this(
                    snapshot.SourceRoutineExerciseId,
                    snapshot.ExerciseId,
                    snapshot.ExerciseCode,
                    snapshot.Name,
                    snapshot.Order,
                    snapshot.Priority,
                    snapshot.MovementPattern,
                    snapshot.ExerciseType,
                    snapshot.PrimaryMuscleGroup,
                    snapshot.SecondaryMuscleGroups,
                    snapshot.RequiredEquipmentIds,
                    snapshot.Prescription)
            {
            }

            public WorkingExercise(
                Guid sourceRoutineExerciseId,
                Guid exerciseId,
                string? exerciseCode,
                string name,
                int order,
                AdaptiveTrainingExercisePriority priority,
                PatronMovimiento? movementPattern,
                TipoEjercicio? exerciseType,
                string primaryMuscleGroup,
                IReadOnlyCollection<string> secondaryMuscleGroups,
                IReadOnlyCollection<Guid> requiredEquipmentIds,
                AdaptiveTrainingPrescription prescription)
            {
                SourceRoutineExerciseId = sourceRoutineExerciseId;
                ExerciseId = exerciseId;
                ExerciseCode = exerciseCode;
                Name = name;
                Order = order;
                Priority = priority;
                MovementPattern = movementPattern;
                ExerciseType = exerciseType;
                PrimaryMuscleGroup = primaryMuscleGroup;
                SecondaryMuscleGroups = secondaryMuscleGroups;
                RequiredEquipmentIds = requiredEquipmentIds;
                Prescription = prescription;
            }
        }
    }
}

