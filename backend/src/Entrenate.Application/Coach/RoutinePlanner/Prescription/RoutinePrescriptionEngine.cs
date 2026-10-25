using Entrenate.Application.Coach.RoutinePlanner.ExerciseSelection;
using Entrenate.Application.Coach.RoutinePlanner.WeeklyStructure;
using Entrenate.Domain.Enums;

namespace Entrenate.Application.Coach.RoutinePlanner.Prescription
{
    public static class RoutinePrescriptionEngine
    {
        public const string PolicyVersion = "2.3";

        private const int PreparationSeconds = 5 * 60;
        private const int TransitionSeconds = 60;
        private const int SetExecutionSeconds = 45;
        private const decimal DurationTolerance = 1.10m;

        public static WeeklyRoutinePrescription Prescribe(
            WeeklyExerciseSelection selection,
            RoutinePrescriptionProfile profile)
        {
            ArgumentNullException.ThrowIfNull(selection);
            ArgumentNullException.ThrowIfNull(profile);

            ValidateProfile(profile);

            var selectionIsInvalid =
                selection.Feasibility ==
                    ExerciseSelectionFeasibility.NotFeasible ||
                selection.Sessions
                    .SelectMany(session => session.Slots)
                    .Any(slot => slot.IsRequired && slot.Exercise is null);

            if (selectionIsInvalid)
            {
                return new WeeklyRoutinePrescription(
                    RoutinePrescriptionFeasibility.NotFeasible,
                    [],
                    [],
                    selection.Diagnostics,
                    [
                        new RoutinePrescriptionDiagnostic(
                            RoutinePrescriptionDiagnosticCode
                                .SelectionNotFeasible)
                    ]);
            }

            var policy = GetVolumePolicy(profile.ExperienceLevel);
            var rir = GetRir(profile.ExperienceLevel);
            var prescriptionDiagnostics =
                new List<RoutinePrescriptionDiagnostic>();
            var sessionPlans = BuildSessionPlans(
                selection,
                profile.Goal,
                policy,
                rir);

            AllocateWeeklySeries(
                sessionPlans,
                profile.Goal,
                policy,
                prescriptionDiagnostics);

            foreach (var session in sessionPlans)
            {
                FitSessionToDuration(
                    session,
                    profile.TargetSessionDurationMinutes,
                    prescriptionDiagnostics);
            }

            var prescribedSessions = sessionPlans
                .Select(session => ToPrescription(
                    session,
                    profile.TargetSessionDurationMinutes))
                .ToArray();
            var weeklyVolumes = CalculateWeeklyMuscleVolumes(
                prescribedSessions);
            var feasibility = DetermineFeasibility(
                selection.Diagnostics,
                prescriptionDiagnostics);

            return new WeeklyRoutinePrescription(
                feasibility,
                prescribedSessions,
                weeklyVolumes,
                selection.Diagnostics,
                prescriptionDiagnostics);
        }

        private static List<SessionPlan> BuildSessionPlans(
            WeeklyExerciseSelection selection,
            ObjetivoEntrenamiento goal,
            VolumePolicy volumePolicy,
            RirRange rir)
        {
            return selection.Sessions
                .OrderBy(session => session.DayOrder)
                .Select(session => new SessionPlan(
                    session.DayOrder,
                    session.ScheduledDay,
                    session.Focus,
                    session.Variant,
                    session.Slots
                        .Where(slot => slot.Exercise is not null)
                        .OrderBy(slot => slot.Position)
                        .Select(slot => CreateExercisePlan(
                            session.DayOrder,
                            slot,
                            goal,
                            volumePolicy,
                            rir))
                        .ToList()))
                .ToList();
        }

        private static ExercisePlan CreateExercisePlan(
            int dayOrder,
            RoutineExerciseSlotSelection slot,
            ObjetivoEntrenamiento goal,
            VolumePolicy volumePolicy,
            RirRange rir)
        {
            var exercise = slot.Exercise!;
            var prescription = GetPrescription(goal, slot.Role);

            return new ExercisePlan(
                dayOrder,
                slot.Position,
                exercise.ExerciseId,
                exercise.Code,
                exercise.Name,
                exercise.PrimaryMuscleGroup,
                exercise.SecondaryMuscleGroups,
                exercise.MovementPattern,
                exercise.ExerciseType,
                slot.Role,
                slot.IsRequired,
                volumePolicy.MinSetsPerExercise,
                volumePolicy.MaxSetsPerExercise,
                prescription.MinReps,
                prescription.MaxReps,
                rir.Min,
                rir.Max,
                prescription.RestSeconds);
        }

        private static void AllocateWeeklySeries(
            IReadOnlyCollection<SessionPlan> sessions,
            ObjetivoEntrenamiento goal,
            VolumePolicy policy,
            ICollection<RoutinePrescriptionDiagnostic> diagnostics)
        {
            var exercises = sessions
                .SelectMany(session => session.Exercises)
                .ToArray();

            foreach (var group in exercises
                .Where(exercise =>
                    exercise.ExerciseType == TipoEjercicio.Compuesto)
                .GroupBy(exercise => exercise.MovementPattern)
                .OrderBy(group => group.Key))
            {
                AllocateSeries(
                    group.OrderBy(exercise => exercise.DayOrder)
                        .ThenBy(exercise => exercise.SlotPosition)
                        .ToArray(),
                    SelectTarget(
                        goal,
                        policy.MinPrimaryWeeklySets,
                        policy.MaxPrimaryWeeklySets),
                    policy.MinPrimaryWeeklySets,
                    policy.MaxPrimaryWeeklySets,
                    diagnostics,
                    group.Key,
                    null);
            }

            foreach (var group in exercises
                .Where(exercise =>
                    exercise.ExerciseType == TipoEjercicio.Aislamiento)
                .GroupBy(
                    exercise => exercise.PrimaryMuscleGroup,
                    StringComparer.OrdinalIgnoreCase)
                .OrderBy(group => group.Key, StringComparer.Ordinal))
            {
                AllocateSeries(
                    group.OrderBy(exercise => exercise.DayOrder)
                        .ThenBy(exercise => exercise.SlotPosition)
                        .ToArray(),
                    SelectTarget(
                        goal,
                        policy.MinAccessoryWeeklySets,
                        policy.MaxAccessoryWeeklySets),
                    policy.MinAccessoryWeeklySets,
                    policy.MaxAccessoryWeeklySets,
                    diagnostics,
                    null,
                    group.Key);
            }
        }

        private static void AllocateSeries(
            IReadOnlyList<ExercisePlan> exercises,
            int weeklyTarget,
            int minimumWeeklySets,
            int maximumWeeklySets,
            ICollection<RoutinePrescriptionDiagnostic> diagnostics,
            PatronMovimiento? movementPattern,
            string? muscleGroup)
        {
            var minimumTotal = exercises.Sum(x => x.MinSets);
            var maximumTotal = exercises.Sum(x => x.MaxSets);
            var allocatedTarget = Math.Clamp(
                weeklyTarget,
                minimumTotal,
                maximumTotal);

            if (allocatedTarget < minimumWeeklySets ||
                allocatedTarget > maximumWeeklySets)
            {
                diagnostics.Add(new RoutinePrescriptionDiagnostic(
                    RoutinePrescriptionDiagnosticCode
                        .WeeklyVolumeTargetUnreachable,
                    MovementPattern: movementPattern,
                    MuscleGroup: muscleGroup));
            }

            foreach (var exercise in exercises)
            {
                exercise.Sets = exercise.MinSets;
            }

            var remainingSets = allocatedTarget - minimumTotal;

            while (remainingSets > 0)
            {
                foreach (var exercise in exercises)
                {
                    if (remainingSets == 0)
                    {
                        break;
                    }

                    if (exercise.Sets >= exercise.MaxSets)
                    {
                        continue;
                    }

                    exercise.Sets++;
                    remainingSets--;
                }
            }
        }

        private static void FitSessionToDuration(
            SessionPlan session,
            int targetDurationMinutes,
            ICollection<RoutinePrescriptionDiagnostic> diagnostics)
        {
            var maximumDurationSeconds =
                targetDurationMinutes * 60 * DurationTolerance;

            if (!ExceedsDuration(session, maximumDurationSeconds))
            {
                return;
            }

            foreach (var exercise in session.Exercises
                .Where(exercise =>
                    exercise.SlotRole == ExerciseSlotRole.Accessory &&
                    !exercise.IsRequired)
                .OrderByDescending(exercise => exercise.SlotPosition)
                .ToArray())
            {
                session.Exercises.Remove(exercise);
                diagnostics.Add(new RoutinePrescriptionDiagnostic(
                    RoutinePrescriptionDiagnosticCode
                        .OptionalExerciseRemovedForDuration,
                    session.DayOrder,
                    exercise.ExerciseId));

                if (!ExceedsDuration(session, maximumDurationSeconds))
                {
                    return;
                }
            }

            ReduceSetsUntilWithinDuration(
                session,
                maximumDurationSeconds,
                exercise =>
                    exercise.SlotRole == ExerciseSlotRole.Accessory,
                diagnostics);
            ReduceSetsUntilWithinDuration(
                session,
                maximumDurationSeconds,
                exercise =>
                    exercise.SlotRole ==
                        ExerciseSlotRole.SecondaryCompound,
                diagnostics);
            ReduceSetsUntilWithinDuration(
                session,
                maximumDurationSeconds,
                exercise =>
                    exercise.SlotRole == ExerciseSlotRole.PrimaryCompound,
                diagnostics);

            if (ExceedsDuration(session, maximumDurationSeconds))
            {
                diagnostics.Add(new RoutinePrescriptionDiagnostic(
                    RoutinePrescriptionDiagnosticCode
                        .SessionDurationExceeded,
                    session.DayOrder));
            }
        }

        private static void ReduceSetsUntilWithinDuration(
            SessionPlan session,
            decimal maximumDurationSeconds,
            Func<ExercisePlan, bool> predicate,
            ICollection<RoutinePrescriptionDiagnostic> diagnostics)
        {
            while (ExceedsDuration(session, maximumDurationSeconds))
            {
                var reducibleExercises = session.Exercises
                    .Where(exercise =>
                        predicate(exercise) &&
                        exercise.Sets > exercise.MinSets)
                    .OrderByDescending(exercise => exercise.SlotPosition)
                    .ToArray();

                if (reducibleExercises.Length == 0)
                {
                    return;
                }

                foreach (var exercise in reducibleExercises)
                {
                    if (!ExceedsDuration(session, maximumDurationSeconds))
                    {
                        return;
                    }

                    exercise.Sets--;

                    if (!diagnostics.Any(diagnostic =>
                        diagnostic.Code ==
                            RoutinePrescriptionDiagnosticCode
                                .ExerciseSetsReducedForDuration &&
                        diagnostic.DayOrder == session.DayOrder &&
                        diagnostic.ExerciseId == exercise.ExerciseId))
                    {
                        diagnostics.Add(new RoutinePrescriptionDiagnostic(
                            RoutinePrescriptionDiagnosticCode
                                .ExerciseSetsReducedForDuration,
                            session.DayOrder,
                            exercise.ExerciseId));
                    }
                }
            }
        }

        private static bool ExceedsDuration(
            SessionPlan session,
            decimal maximumDurationSeconds)
        {
            return CalculateDurationSeconds(session.Exercises) >
                maximumDurationSeconds;
        }

        private static int CalculateDurationSeconds(
            IReadOnlyCollection<ExercisePlan> exercises)
        {
            var transitionCount = Math.Max(0, exercises.Count - 1);

            return PreparationSeconds +
                transitionCount * TransitionSeconds +
                exercises.Sum(exercise =>
                    SetExecutionSeconds * exercise.Sets +
                    exercise.RestSeconds * (exercise.Sets - 1));
        }

        private static PrescribedRoutineSession ToPrescription(
            SessionPlan session,
            int targetDurationMinutes)
        {
            var exercises = session.Exercises
                .OrderBy(exercise => exercise.SlotPosition)
                .Select((exercise, index) =>
                    new PrescribedRoutineExercise(
                        exercise.ExerciseId,
                        exercise.Code,
                        exercise.Name,
                        exercise.PrimaryMuscleGroup,
                        exercise.SecondaryMuscleGroups,
                        exercise.MovementPattern,
                        exercise.ExerciseType,
                        exercise.SlotRole,
                        exercise.IsRequired,
                        exercise.Sets,
                        exercise.MinReps,
                        exercise.MaxReps,
                        exercise.MinRir,
                        exercise.MaxRir,
                        exercise.RestSeconds,
                        index + 1))
                .ToArray();

            return new PrescribedRoutineSession(
                session.DayOrder,
                session.ScheduledDay,
                session.Focus,
                session.Variant,
                targetDurationMinutes,
                CalculateDurationSeconds(session.Exercises),
                exercises);
        }

        private static IReadOnlyList<WeeklyMuscleVolume>
            CalculateWeeklyMuscleVolumes(
                IReadOnlyCollection<PrescribedRoutineSession> sessions)
        {
            var volumes = new Dictionary<string, decimal>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var exercise in sessions
                .SelectMany(session => session.Exercises))
            {
                AddVolume(
                    volumes,
                    exercise.PrimaryMuscleGroup,
                    exercise.Sets);

                if (exercise.ExerciseType != TipoEjercicio.Compuesto)
                {
                    continue;
                }

                foreach (var secondaryMuscleGroup in
                    exercise.SecondaryMuscleGroups
                        .Where(group => !string.IsNullOrWhiteSpace(group))
                        .Select(group => group.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    AddVolume(
                        volumes,
                        secondaryMuscleGroup,
                        exercise.Sets * 0.5m);
                }
            }

            return volumes
                .OrderBy(volume => volume.Key, StringComparer.Ordinal)
                .Select(volume => new WeeklyMuscleVolume(
                    volume.Key,
                    volume.Value))
                .ToArray();
        }

        private static void AddVolume(
            IDictionary<string, decimal> volumes,
            string muscleGroup,
            decimal sets)
        {
            volumes[muscleGroup] = volumes.TryGetValue(
                muscleGroup,
                out var current)
                    ? current + sets
                    : sets;
        }

        private static RoutinePrescriptionFeasibility DetermineFeasibility(
            IReadOnlyCollection<ExerciseSelectionDiagnostic>
                selectionDiagnostics,
            IReadOnlyCollection<RoutinePrescriptionDiagnostic>
                prescriptionDiagnostics)
        {
            if (prescriptionDiagnostics.Any(diagnostic =>
                diagnostic.Code == RoutinePrescriptionDiagnosticCode
                    .SessionDurationExceeded ||
                diagnostic.Code == RoutinePrescriptionDiagnosticCode
                    .SelectionNotFeasible))
            {
                return RoutinePrescriptionFeasibility.NotFeasible;
            }

            return selectionDiagnostics.Count > 0 ||
                prescriptionDiagnostics.Count > 0
                    ? RoutinePrescriptionFeasibility.FeasibleWithWarnings
                    : RoutinePrescriptionFeasibility.Feasible;
        }

        private static int SelectTarget(
            ObjetivoEntrenamiento goal,
            int minimum,
            int maximum)
        {
            return goal switch
            {
                ObjetivoEntrenamiento.GanarMasaMuscular => maximum,
                ObjetivoEntrenamiento
                    .GanarmasaMuscularYFuerza => (minimum + maximum) / 2,
                ObjetivoEntrenamiento.GanarFuerza => minimum,
                ObjetivoEntrenamiento
                    .AcondicionamientoGeneral => minimum,
                _ => throw new ArgumentOutOfRangeException(nameof(goal))
            };
        }

        private static PrescriptionValues GetPrescription(
            ObjetivoEntrenamiento goal,
            ExerciseSlotRole role)
        {
            var isPrimary = role == ExerciseSlotRole.PrimaryCompound;
            var isSecondaryCompound =
                role == ExerciseSlotRole.SecondaryCompound;

            return goal switch
            {
                ObjetivoEntrenamiento.GanarFuerza => isPrimary
                    ? new PrescriptionValues(4, 6, 180)
                    : new PrescriptionValues(
                        6,
                        10,
                        isSecondaryCompound ? 120 : 90),
                ObjetivoEntrenamiento.GanarMasaMuscular => isPrimary
                    ? new PrescriptionValues(6, 12, 120)
                    : new PrescriptionValues(
                        10,
                        15,
                        isSecondaryCompound ? 90 : 60),
                ObjetivoEntrenamiento.GanarmasaMuscularYFuerza =>
                    isPrimary
                        ? new PrescriptionValues(5, 8, 150)
                        : new PrescriptionValues(8, 12, 90),
                ObjetivoEntrenamiento.AcondicionamientoGeneral =>
                    isPrimary
                        ? new PrescriptionValues(8, 12, 90)
                        : new PrescriptionValues(10, 15, 60),
                _ => throw new ArgumentOutOfRangeException(nameof(goal))
            };
        }

        private static VolumePolicy GetVolumePolicy(
            NivelExperiencia experienceLevel)
        {
            return experienceLevel switch
            {
                NivelExperiencia.SinExperiencia =>
                    new VolumePolicy(2, 2, 4, 6, 0, 2),
                NivelExperiencia.Principiante =>
                    new VolumePolicy(2, 3, 6, 8, 2, 4),
                NivelExperiencia.Intermedio =>
                    new VolumePolicy(3, 3, 8, 12, 4, 6),
                NivelExperiencia.Avanzado =>
                    new VolumePolicy(3, 4, 10, 14, 4, 8),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(experienceLevel))
            };
        }

        private static RirRange GetRir(
            NivelExperiencia experienceLevel)
        {
            return experienceLevel switch
            {
                NivelExperiencia.SinExperiencia => new RirRange(3, 4),
                NivelExperiencia.Principiante => new RirRange(2, 3),
                NivelExperiencia.Intermedio => new RirRange(2, 3),
                NivelExperiencia.Avanzado => new RirRange(1, 3),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(experienceLevel))
            };
        }

        private static void ValidateProfile(
            RoutinePrescriptionProfile profile)
        {
            if (!Enum.IsDefined(profile.Goal))
            {
                throw new ArgumentOutOfRangeException(nameof(profile.Goal));
            }

            if (!Enum.IsDefined(profile.ExperienceLevel))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(profile.ExperienceLevel));
            }

            if (profile.TargetSessionDurationMinutes is < 15 or > 240)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(profile.TargetSessionDurationMinutes));
            }
        }

        private sealed record VolumePolicy(
            int MinSetsPerExercise,
            int MaxSetsPerExercise,
            int MinPrimaryWeeklySets,
            int MaxPrimaryWeeklySets,
            int MinAccessoryWeeklySets,
            int MaxAccessoryWeeklySets);

        private sealed record RirRange(int Min, int Max);

        private sealed record PrescriptionValues(
            int MinReps,
            int MaxReps,
            int RestSeconds);

        private sealed class SessionPlan
        {
            public int DayOrder { get; }
            public DiaSemana ScheduledDay { get; }
            public RoutineSessionFocus Focus { get; }
            public RoutineSessionVariant Variant { get; }
            public List<ExercisePlan> Exercises { get; }

            public SessionPlan(
                int dayOrder,
                DiaSemana scheduledDay,
                RoutineSessionFocus focus,
                RoutineSessionVariant variant,
                List<ExercisePlan> exercises)
            {
                DayOrder = dayOrder;
                ScheduledDay = scheduledDay;
                Focus = focus;
                Variant = variant;
                Exercises = exercises;
            }
        }

        private sealed class ExercisePlan
        {
            public int DayOrder { get; }
            public int SlotPosition { get; }
            public Guid ExerciseId { get; }
            public string Code { get; }
            public string Name { get; }
            public string PrimaryMuscleGroup { get; }
            public IReadOnlyCollection<string> SecondaryMuscleGroups { get; }
            public PatronMovimiento MovementPattern { get; }
            public TipoEjercicio ExerciseType { get; }
            public ExerciseSlotRole SlotRole { get; }
            public bool IsRequired { get; }
            public int MinSets { get; }
            public int MaxSets { get; }
            public int MinReps { get; }
            public int MaxReps { get; }
            public int MinRir { get; }
            public int MaxRir { get; }
            public int RestSeconds { get; }
            public int Sets { get; set; }

            public ExercisePlan(
                int dayOrder,
                int slotPosition,
                Guid exerciseId,
                string code,
                string name,
                string primaryMuscleGroup,
                IReadOnlyCollection<string> secondaryMuscleGroups,
                PatronMovimiento movementPattern,
                TipoEjercicio exerciseType,
                ExerciseSlotRole slotRole,
                bool isRequired,
                int minSets,
                int maxSets,
                int minReps,
                int maxReps,
                int minRir,
                int maxRir,
                int restSeconds)
            {
                DayOrder = dayOrder;
                SlotPosition = slotPosition;
                ExerciseId = exerciseId;
                Code = code;
                Name = name;
                PrimaryMuscleGroup = primaryMuscleGroup;
                SecondaryMuscleGroups = secondaryMuscleGroups;
                MovementPattern = movementPattern;
                ExerciseType = exerciseType;
                SlotRole = slotRole;
                IsRequired = isRequired;
                MinSets = minSets;
                MaxSets = maxSets;
                MinReps = minReps;
                MaxReps = maxReps;
                MinRir = minRir;
                MaxRir = maxRir;
                RestSeconds = restSeconds;
            }
        }
    }
}
