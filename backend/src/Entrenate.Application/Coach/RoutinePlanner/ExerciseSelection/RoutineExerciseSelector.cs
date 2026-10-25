using Entrenate.Application.Coach.RoutinePlanner.WeeklyStructure;
using Entrenate.Domain.Enums;

namespace Entrenate.Application.Coach.RoutinePlanner.ExerciseSelection
{
    public static class RoutineExerciseSelector
    {
        public const string PolicyVersion = "2.2";

        private static readonly IReadOnlyDictionary<
            RoutineSessionFocus,
            IReadOnlyList<SlotDefinition>> SlotsByFocus =
                new Dictionary<
                    RoutineSessionFocus,
                    IReadOnlyList<SlotDefinition>>
                {
                    [RoutineSessionFocus.FullBody] =
                    [
                        Primary(PatronMovimiento.DominanteRodilla),
                        Primary(
                            PatronMovimiento.BisagraCadera,
                            PatronMovimiento.ExtensionCadera),
                        Primary(
                            PatronMovimiento.EmpujeHorizontal,
                            PatronMovimiento.EmpujeVertical),
                        Primary(
                            PatronMovimiento.TraccionHorizontal,
                            PatronMovimiento.TraccionVertical),
                        Accessory(
                            true,
                            PatronMovimiento.CoreAntiExtension,
                            PatronMovimiento.CoreFlexion)
                    ],
                    [RoutineSessionFocus.Upper] =
                    [
                        Primary(PatronMovimiento.EmpujeHorizontal),
                        Primary(PatronMovimiento.TraccionHorizontal),
                        Secondary(PatronMovimiento.EmpujeVertical),
                        Secondary(PatronMovimiento.TraccionVertical),
                        Accessory(
                            false,
                            PatronMovimiento.AbduccionHombro),
                        Accessory(false, PatronMovimiento.FlexionCodo),
                        Accessory(false, PatronMovimiento.ExtensionCodo)
                    ],
                    [RoutineSessionFocus.Lower] =
                    [
                        Primary(PatronMovimiento.DominanteRodilla),
                        Primary(
                            PatronMovimiento.BisagraCadera,
                            PatronMovimiento.ExtensionCadera),
                        Accessory(
                            false,
                            PatronMovimiento.ExtensionRodilla),
                        Accessory(
                            false,
                            PatronMovimiento.FlexionRodilla),
                        Accessory(false, PatronMovimiento.FlexionPlantar),
                        Accessory(
                            true,
                            PatronMovimiento.CoreAntiExtension,
                            PatronMovimiento.CoreFlexion)
                    ],
                    [RoutineSessionFocus.Push] =
                    [
                        Primary(PatronMovimiento.EmpujeHorizontal),
                        Secondary(PatronMovimiento.EmpujeVertical),
                        Accessory(
                            false,
                            PatronMovimiento.AbduccionHombro),
                        Accessory(false, PatronMovimiento.ExtensionCodo)
                    ],
                    [RoutineSessionFocus.Pull] =
                    [
                        Primary(PatronMovimiento.TraccionVertical),
                        Secondary(PatronMovimiento.TraccionHorizontal),
                        Accessory(false, PatronMovimiento.FlexionCodo),
                        Accessory(
                            false,
                            PatronMovimiento.RotacionExternaHombro)
                    ],
                    [RoutineSessionFocus.Legs] =
                    [
                        Primary(PatronMovimiento.DominanteRodilla),
                        Primary(
                            PatronMovimiento.BisagraCadera,
                            PatronMovimiento.ExtensionCadera),
                        Accessory(
                            false,
                            PatronMovimiento.ExtensionRodilla),
                        Accessory(
                            false,
                            PatronMovimiento.FlexionRodilla),
                        Accessory(false, PatronMovimiento.FlexionPlantar),
                        Accessory(
                            true,
                            PatronMovimiento.CoreAntiExtension,
                            PatronMovimiento.CoreFlexion)
                    ],
                    [RoutineSessionFocus.CoreCalves] =
                    [
                        Accessory(
                            true,
                            PatronMovimiento.CoreAntiExtension,
                            PatronMovimiento.CoreFlexion),
                        Accessory(true, PatronMovimiento.FlexionPlantar)
                    ]
                };

        public static WeeklyExerciseSelection Select(
            WeeklyRoutineStructure weeklyStructure,
            IReadOnlyCollection<PlannerExerciseCandidate> catalog,
            IReadOnlyCollection<Guid> availableEquipmentIds)
        {
            ArgumentNullException.ThrowIfNull(weeklyStructure);
            ArgumentNullException.ThrowIfNull(catalog);
            ArgumentNullException.ThrowIfNull(availableEquipmentIds);

            var availableEquipment = availableEquipmentIds.ToHashSet();
            var eligibleExercises = catalog
                .Where(candidate => IsEligible(
                    candidate,
                    availableEquipment))
                .ToArray();
            var patternUsage = new Dictionary<PatronMovimiento, int>();
            var muscleGroupUsage = new Dictionary<string, int>(
                StringComparer.OrdinalIgnoreCase);
            var exerciseUsage = new Dictionary<Guid, int>();
            var focusExerciseUsage = new Dictionary<
                RoutineSessionFocus,
                Dictionary<Guid, int>>();
            var diagnostics = new List<ExerciseSelectionDiagnostic>();
            var sessions = new List<RoutineSessionExerciseSelection>();
            HashSet<Guid> previousSessionExercises = [];

            foreach (var day in weeklyStructure.Days.OrderBy(x => x.Order))
            {
                var definitions = SlotsByFocus[day.Focus];
                var selectedInSession = new HashSet<Guid>();
                var slots = new List<RoutineExerciseSlotSelection>();
                var equivalentFocusUsage = focusExerciseUsage.GetValueOrDefault(
                    day.Focus);

                for (var index = 0; index < definitions.Count; index++)
                {
                    var definition = definitions[index];
                    var candidates = eligibleExercises
                        .Where(candidate =>
                            candidate.ExerciseType ==
                                definition.RequiredExerciseType &&
                            definition.AcceptedPatterns.Contains(
                                candidate.MovementPattern!.Value) &&
                            !selectedInSession.Contains(
                                candidate.ExerciseId))
                        .OrderBy(candidate => GetUsage(
                            patternUsage,
                            candidate.MovementPattern!.Value))
                        .ThenBy(candidate => GetUsage(
                            muscleGroupUsage,
                            candidate.PrimaryMuscleGroup))
                        .ThenBy(candidate => GetUsage(
                            equivalentFocusUsage,
                            candidate.ExerciseId))
                        .ThenBy(candidate => previousSessionExercises.Contains(
                            candidate.ExerciseId))
                        .ThenBy(candidate => GetUsage(
                            exerciseUsage,
                            candidate.ExerciseId))
                        .ThenBy(candidate => candidate.Code,
                            StringComparer.Ordinal)
                        .ThenBy(candidate => candidate.ExerciseId)
                        .ToArray();
                    var selected = candidates.FirstOrDefault();
                    SelectedRoutineExercise? selectedExercise = null;

                    if (selected is null)
                    {
                        diagnostics.Add(new ExerciseSelectionDiagnostic(
                            definition.IsRequired
                                ? ExerciseSelectionDiagnosticCode
                                    .MissingRequiredExercise
                                : ExerciseSelectionDiagnosticCode
                                    .MissingOptionalExercise,
                            day.Order,
                            index + 1));
                    }
                    else
                    {
                        if (GetUsage(
                            equivalentFocusUsage,
                            selected.ExerciseId) > 0)
                        {
                            diagnostics.Add(new ExerciseSelectionDiagnostic(
                                ExerciseSelectionDiagnosticCode
                                    .ExerciseReusedAcrossVariants,
                                day.Order,
                                index + 1));
                        }

                        selectedInSession.Add(selected.ExerciseId);
                        Increment(
                            patternUsage,
                            selected.MovementPattern!.Value);
                        Increment(
                            muscleGroupUsage,
                            selected.PrimaryMuscleGroup);
                        Increment(exerciseUsage, selected.ExerciseId);

                        selectedExercise = new SelectedRoutineExercise(
                            selected.ExerciseId,
                            selected.Code!,
                            selected.Name,
                            selected.PrimaryMuscleGroup,
                            selected.MovementPattern.Value,
                            selected.ExerciseType!.Value);
                    }

                    slots.Add(new RoutineExerciseSlotSelection(
                        index + 1,
                        definition.Role,
                        definition.IsRequired,
                        definition.AcceptedPatterns,
                        definition.RequiredExerciseType,
                        selectedExercise));
                }

                if (!focusExerciseUsage.TryGetValue(
                    day.Focus,
                    out var currentFocusUsage))
                {
                    currentFocusUsage = [];
                    focusExerciseUsage.Add(day.Focus, currentFocusUsage);
                }

                foreach (var exerciseId in selectedInSession)
                {
                    Increment(currentFocusUsage, exerciseId);
                }

                sessions.Add(new RoutineSessionExerciseSelection(
                    day.Order,
                    day.ScheduledDay,
                    day.Focus,
                    day.Variant,
                    slots));
                previousSessionExercises = selectedInSession;
            }

            return new WeeklyExerciseSelection(
                DetermineFeasibility(diagnostics),
                sessions,
                diagnostics);
        }

        private static bool IsEligible(
            PlannerExerciseCandidate candidate,
            IReadOnlySet<Guid> availableEquipmentIds)
        {
            return candidate.IsActive &&
                candidate.ExerciseId != Guid.Empty &&
                !string.IsNullOrWhiteSpace(candidate.Code) &&
                !string.IsNullOrWhiteSpace(candidate.Name) &&
                !string.IsNullOrWhiteSpace(candidate.PrimaryMuscleGroup) &&
                candidate.SecondaryMuscleGroups is not null &&
                candidate.MovementPattern.HasValue &&
                candidate.ExerciseType.HasValue &&
                candidate.EquipmentRequirements is not null &&
                candidate.EquipmentRequirements.All(requirement =>
                    requirement.EquipmentId != Guid.Empty &&
                    requirement.IsActive &&
                    availableEquipmentIds.Contains(
                        requirement.EquipmentId));
        }

        private static ExerciseSelectionFeasibility DetermineFeasibility(
            IReadOnlyCollection<ExerciseSelectionDiagnostic> diagnostics)
        {
            if (diagnostics.Any(x =>
                x.Code == ExerciseSelectionDiagnosticCode
                    .MissingRequiredExercise))
            {
                return ExerciseSelectionFeasibility.NotFeasible;
            }

            return diagnostics.Count > 0
                ? ExerciseSelectionFeasibility.FeasibleWithWarnings
                : ExerciseSelectionFeasibility.Feasible;
        }

        private static int GetUsage<TKey>(
            IReadOnlyDictionary<TKey, int>? usage,
            TKey key)
            where TKey : notnull
        {
            return usage is not null && usage.TryGetValue(key, out var count)
                ? count
                : 0;
        }

        private static void Increment<TKey>(
            IDictionary<TKey, int> usage,
            TKey key)
            where TKey : notnull
        {
            usage[key] = usage.TryGetValue(key, out var count)
                ? count + 1
                : 1;
        }

        private static SlotDefinition Primary(
            params PatronMovimiento[] patterns)
        {
            return new SlotDefinition(
                ExerciseSlotRole.PrimaryCompound,
                true,
                patterns,
                TipoEjercicio.Compuesto);
        }

        private static SlotDefinition Secondary(
            params PatronMovimiento[] patterns)
        {
            return new SlotDefinition(
                ExerciseSlotRole.SecondaryCompound,
                true,
                patterns,
                TipoEjercicio.Compuesto);
        }

        private static SlotDefinition Accessory(
            bool isRequired,
            params PatronMovimiento[] patterns)
        {
            return new SlotDefinition(
                ExerciseSlotRole.Accessory,
                isRequired,
                patterns,
                TipoEjercicio.Aislamiento);
        }

        private sealed record SlotDefinition(
            ExerciseSlotRole Role,
            bool IsRequired,
            IReadOnlyList<PatronMovimiento> AcceptedPatterns,
            TipoEjercicio RequiredExerciseType);
    }
}
