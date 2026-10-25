using Entrenate.Application.Coach.RoutinePlanner.ExerciseSelection;
using Entrenate.Application.Coach.RoutinePlanner.WeeklyStructure;
using Entrenate.Domain.Enums;
using Xunit;

namespace Entrenate.Application.UnitTests.Coach.RoutinePlanner.ExerciseSelection
{
    public class RoutineExerciseSelectorTests
    {
        private static readonly Guid AvailableEquipmentId = CreateGuid(900);
        private static readonly Guid UnavailableEquipmentId = CreateGuid(901);

        [Fact]
        public void Select_OnlyUsesActiveCompatibleExercisesWithCompleteMetadata()
        {
            var structure = CreateStructure(
                (RoutineSessionFocus.CoreCalves,
                    RoutineSessionVariant.None));
            var catalog = new[]
            {
                Candidate(
                    1,
                    "plank",
                    "Core",
                    PatronMovimiento.CoreAntiExtension,
                    TipoEjercicio.Aislamiento),
                Candidate(
                    2,
                    "inactive-calf",
                    "Gemelos",
                    PatronMovimiento.FlexionPlantar,
                    TipoEjercicio.Aislamiento,
                    isActive: false),
                Candidate(
                    3,
                    "unavailable-calf",
                    "Gemelos",
                    PatronMovimiento.FlexionPlantar,
                    TipoEjercicio.Aislamiento,
                    equipment:
                    [
                        new ExerciseEquipmentRequirement(
                            UnavailableEquipmentId,
                            true)
                    ]),
                Candidate(
                    4,
                    "inactive-equipment-calf",
                    "Gemelos",
                    PatronMovimiento.FlexionPlantar,
                    TipoEjercicio.Aislamiento,
                    equipment:
                    [
                        new ExerciseEquipmentRequirement(
                            AvailableEquipmentId,
                            false)
                    ]),
                Candidate(
                    5,
                    "compatible-calf",
                    "Gemelos",
                    PatronMovimiento.FlexionPlantar,
                    TipoEjercicio.Aislamiento,
                    equipment:
                    [
                        new ExerciseEquipmentRequirement(
                            AvailableEquipmentId,
                            true)
                    ])
            };

            var result = RoutineExerciseSelector.Select(
                structure,
                catalog,
                [AvailableEquipmentId]);

            Assert.Equal(
                ExerciseSelectionFeasibility.Feasible,
                result.Feasibility);
            Assert.Equal(
                ["plank", "compatible-calf"],
                result.Sessions[0].Slots
                    .Select(slot => slot.Exercise!.Code));
        }

        [Fact]
        public void Select_CoversFullBodyRequiredSlots()
        {
            var result = RoutineExerciseSelector.Select(
                CreateStructure(
                    (RoutineSessionFocus.FullBody,
                        RoutineSessionVariant.A)),
                CreateCompleteCatalog(),
                []);

            var slots = result.Sessions[0].Slots;

            Assert.Equal(
                ExerciseSelectionFeasibility.Feasible,
                result.Feasibility);
            Assert.Equal(5, slots.Count);
            Assert.All(slots, slot => Assert.NotNull(slot.Exercise));
            Assert.All(slots, slot => Assert.True(slot.IsRequired));
            Assert.Contains(
                slots,
                slot => slot.Exercise!.MovementPattern ==
                    PatronMovimiento.DominanteRodilla);
            Assert.Contains(
                slots,
                slot => slot.Exercise!.MovementPattern is
                    PatronMovimiento.BisagraCadera or
                    PatronMovimiento.ExtensionCadera);
            Assert.Contains(
                slots,
                slot => slot.Exercise!.MovementPattern is
                    PatronMovimiento.EmpujeHorizontal or
                    PatronMovimiento.EmpujeVertical);
            Assert.Contains(
                slots,
                slot => slot.Exercise!.MovementPattern is
                    PatronMovimiento.TraccionHorizontal or
                    PatronMovimiento.TraccionVertical);
            Assert.Contains(
                slots,
                slot => slot.Exercise!.MovementPattern is
                    PatronMovimiento.CoreAntiExtension or
                    PatronMovimiento.CoreFlexion);
        }

        [Fact]
        public void Select_CoversUpperAndLowerSessions()
        {
            var result = RoutineExerciseSelector.Select(
                CreateStructure(
                    (RoutineSessionFocus.Upper, RoutineSessionVariant.A),
                    (RoutineSessionFocus.Lower, RoutineSessionVariant.A)),
                CreateCompleteCatalog(),
                []);

            Assert.DoesNotContain(
                result.Diagnostics,
                diagnostic => diagnostic.Code ==
                    ExerciseSelectionDiagnosticCode.MissingRequiredExercise);
            AssertRequiredPatterns(
                result.Sessions[0],
                PatronMovimiento.EmpujeHorizontal,
                PatronMovimiento.TraccionHorizontal,
                PatronMovimiento.EmpujeVertical,
                PatronMovimiento.TraccionVertical);
            AssertRequiredPatternFamily(
                result.Sessions[1],
                PatronMovimiento.DominanteRodilla);
            AssertRequiredPatternFamily(
                result.Sessions[1],
                PatronMovimiento.BisagraCadera,
                PatronMovimiento.ExtensionCadera);
        }

        [Theory]
        [InlineData(
            RoutineSessionFocus.Push,
            PatronMovimiento.EmpujeHorizontal,
            PatronMovimiento.EmpujeVertical)]
        [InlineData(
            RoutineSessionFocus.Pull,
            PatronMovimiento.TraccionVertical,
            PatronMovimiento.TraccionHorizontal)]
        [InlineData(
            RoutineSessionFocus.Legs,
            PatronMovimiento.DominanteRodilla,
            PatronMovimiento.BisagraCadera)]
        public void Select_CoversPplRequiredPatterns(
            RoutineSessionFocus focus,
            PatronMovimiento firstPattern,
            PatronMovimiento secondPattern)
        {
            var result = RoutineExerciseSelector.Select(
                CreateStructure((focus, RoutineSessionVariant.A)),
                CreateCompleteCatalog(),
                []);

            Assert.NotEqual(
                ExerciseSelectionFeasibility.NotFeasible,
                result.Feasibility);
            AssertRequiredPatternFamily(
                result.Sessions[0],
                firstPattern);
            AssertRequiredPatternFamily(
                result.Sessions[0],
                secondPattern,
                focus == RoutineSessionFocus.Legs
                    ? PatronMovimiento.ExtensionCadera
                    : secondPattern);
        }

        [Fact]
        public void Select_CoversCoreCalvesSession()
        {
            var result = RoutineExerciseSelector.Select(
                CreateStructure(
                    (RoutineSessionFocus.CoreCalves,
                        RoutineSessionVariant.None)),
                CreateCompleteCatalog(),
                []);

            Assert.Equal(
                ExerciseSelectionFeasibility.Feasible,
                result.Feasibility);
            AssertRequiredPatternFamily(
                result.Sessions[0],
                PatronMovimiento.CoreAntiExtension,
                PatronMovimiento.CoreFlexion);
            AssertRequiredPatternFamily(
                result.Sessions[0],
                PatronMovimiento.FlexionPlantar);
        }

        [Fact]
        public void Select_RotatesAlternativesAcrossEquivalentVariants()
        {
            var structure = CreateStructure(
                (RoutineSessionFocus.Push, RoutineSessionVariant.A),
                (RoutineSessionFocus.Push, RoutineSessionVariant.B));
            var catalog = new[]
            {
                Candidate(1, "bench-a", "Pecho",
                    PatronMovimiento.EmpujeHorizontal,
                    TipoEjercicio.Compuesto),
                Candidate(2, "bench-b", "Pecho",
                    PatronMovimiento.EmpujeHorizontal,
                    TipoEjercicio.Compuesto),
                Candidate(3, "press-a", "Hombros",
                    PatronMovimiento.EmpujeVertical,
                    TipoEjercicio.Compuesto),
                Candidate(4, "press-b", "Hombros",
                    PatronMovimiento.EmpujeVertical,
                    TipoEjercicio.Compuesto)
            };

            var result = RoutineExerciseSelector.Select(
                structure,
                catalog,
                []);

            Assert.NotEqual(
                result.Sessions[0].Slots[0].Exercise!.ExerciseId,
                result.Sessions[1].Slots[0].Exercise!.ExerciseId);
            Assert.NotEqual(
                result.Sessions[0].Slots[1].Exercise!.ExerciseId,
                result.Sessions[1].Slots[1].Exercise!.ExerciseId);
            Assert.DoesNotContain(
                result.Diagnostics,
                diagnostic => diagnostic.Code ==
                    ExerciseSelectionDiagnosticCode
                        .ExerciseReusedAcrossVariants);
        }

        [Fact]
        public void Select_ReusesExerciseAndWarnsWhenNoAlternativeExists()
        {
            var structure = CreateStructure(
                (RoutineSessionFocus.Push, RoutineSessionVariant.A),
                (RoutineSessionFocus.Push, RoutineSessionVariant.B));
            var catalog = new[]
            {
                Candidate(1, "bench", "Pecho",
                    PatronMovimiento.EmpujeHorizontal,
                    TipoEjercicio.Compuesto),
                Candidate(2, "press", "Hombros",
                    PatronMovimiento.EmpujeVertical,
                    TipoEjercicio.Compuesto)
            };

            var result = RoutineExerciseSelector.Select(
                structure,
                catalog,
                []);

            Assert.NotEqual(
                ExerciseSelectionFeasibility.NotFeasible,
                result.Feasibility);
            Assert.Equal(
                result.Sessions[0].Slots[0].Exercise!.ExerciseId,
                result.Sessions[1].Slots[0].Exercise!.ExerciseId);
            Assert.Equal(
                2,
                result.Diagnostics.Count(diagnostic =>
                    diagnostic.Code == ExerciseSelectionDiagnosticCode
                        .ExerciseReusedAcrossVariants));
        }

        [Fact]
        public void Select_ReturnsNotFeasibleWhenRequiredSlotHasNoCandidate()
        {
            var result = RoutineExerciseSelector.Select(
                CreateStructure(
                    (RoutineSessionFocus.Pull,
                        RoutineSessionVariant.None)),
                [
                    Candidate(1, "row", "Espalda",
                        PatronMovimiento.TraccionHorizontal,
                        TipoEjercicio.Compuesto)
                ],
                []);

            Assert.Equal(
                ExerciseSelectionFeasibility.NotFeasible,
                result.Feasibility);
            Assert.Contains(
                result.Diagnostics,
                diagnostic =>
                    diagnostic.DayOrder == 1 &&
                    diagnostic.SlotPosition == 1 &&
                    diagnostic.Code == ExerciseSelectionDiagnosticCode
                        .MissingRequiredExercise);
            Assert.Null(result.Sessions[0].Slots[0].Exercise);
        }

        [Fact]
        public void Select_IsDeterministicForSameInputs()
        {
            var structure = WeeklyRoutineStructurePlanner.Build(3, []);
            var catalog = CreateCompleteCatalog().Reverse().ToArray();

            var first = RoutineExerciseSelector.Select(
                structure,
                catalog,
                []);
            var second = RoutineExerciseSelector.Select(
                structure,
                catalog,
                []);

            Assert.Equal(
                first.Sessions.SelectMany(session => session.Slots)
                    .Select(slot => slot.Exercise?.ExerciseId),
                second.Sessions.SelectMany(session => session.Slots)
                    .Select(slot => slot.Exercise?.ExerciseId));
            Assert.Equal(first.Diagnostics, second.Diagnostics);
            Assert.Equal(first.Feasibility, second.Feasibility);
        }

        private static WeeklyRoutineStructure CreateStructure(
            params (RoutineSessionFocus Focus,
                RoutineSessionVariant Variant)[] sessions)
        {
            var days = sessions
                .Select((session, index) => new WeeklyRoutineDay(
                    index + 1,
                    (DiaSemana)(index + 1),
                    ScheduledDayOrigin.Derived,
                    session.Focus,
                    session.Variant))
                .ToArray();

            return new WeeklyRoutineStructure(days.Length, days);
        }

        private static PlannerExerciseCandidate[] CreateCompleteCatalog()
        {
            return
            [
                Candidate(1, "bench-a", "Pecho",
                    PatronMovimiento.EmpujeHorizontal,
                    TipoEjercicio.Compuesto),
                Candidate(2, "bench-b", "Pecho",
                    PatronMovimiento.EmpujeHorizontal,
                    TipoEjercicio.Compuesto),
                Candidate(3, "press-a", "Hombros",
                    PatronMovimiento.EmpujeVertical,
                    TipoEjercicio.Compuesto),
                Candidate(4, "press-b", "Hombros",
                    PatronMovimiento.EmpujeVertical,
                    TipoEjercicio.Compuesto),
                Candidate(5, "row-a", "Espalda",
                    PatronMovimiento.TraccionHorizontal,
                    TipoEjercicio.Compuesto),
                Candidate(6, "row-b", "Espalda",
                    PatronMovimiento.TraccionHorizontal,
                    TipoEjercicio.Compuesto),
                Candidate(7, "pull-a", "Espalda",
                    PatronMovimiento.TraccionVertical,
                    TipoEjercicio.Compuesto),
                Candidate(8, "pull-b", "Espalda",
                    PatronMovimiento.TraccionVertical,
                    TipoEjercicio.Compuesto),
                Candidate(9, "squat-a", "Cuádriceps",
                    PatronMovimiento.DominanteRodilla,
                    TipoEjercicio.Compuesto),
                Candidate(10, "squat-b", "Cuádriceps",
                    PatronMovimiento.DominanteRodilla,
                    TipoEjercicio.Compuesto),
                Candidate(11, "hinge-a", "Isquiotibiales",
                    PatronMovimiento.BisagraCadera,
                    TipoEjercicio.Compuesto),
                Candidate(12, "hinge-b", "Isquiotibiales",
                    PatronMovimiento.BisagraCadera,
                    TipoEjercicio.Compuesto),
                Candidate(13, "hip-extension", "Glúteos",
                    PatronMovimiento.ExtensionCadera,
                    TipoEjercicio.Compuesto),
                Candidate(14, "lateral-raise", "Hombros",
                    PatronMovimiento.AbduccionHombro,
                    TipoEjercicio.Aislamiento),
                Candidate(15, "triceps", "Tríceps",
                    PatronMovimiento.ExtensionCodo,
                    TipoEjercicio.Aislamiento),
                Candidate(16, "biceps", "Bíceps",
                    PatronMovimiento.FlexionCodo,
                    TipoEjercicio.Aislamiento),
                Candidate(17, "face-pull", "Hombros",
                    PatronMovimiento.RotacionExternaHombro,
                    TipoEjercicio.Aislamiento),
                Candidate(18, "leg-extension", "Cuádriceps",
                    PatronMovimiento.ExtensionRodilla,
                    TipoEjercicio.Aislamiento),
                Candidate(19, "leg-curl", "Isquiotibiales",
                    PatronMovimiento.FlexionRodilla,
                    TipoEjercicio.Aislamiento),
                Candidate(20, "calf-raise", "Gemelos",
                    PatronMovimiento.FlexionPlantar,
                    TipoEjercicio.Aislamiento),
                Candidate(21, "plank", "Core",
                    PatronMovimiento.CoreAntiExtension,
                    TipoEjercicio.Aislamiento),
                Candidate(22, "crunch", "Core",
                    PatronMovimiento.CoreFlexion,
                    TipoEjercicio.Aislamiento)
            ];
        }

        private static PlannerExerciseCandidate Candidate(
            int id,
            string code,
            string primaryMuscleGroup,
            PatronMovimiento movementPattern,
            TipoEjercicio exerciseType,
            bool isActive = true,
            IReadOnlyCollection<ExerciseEquipmentRequirement>? equipment = null)
        {
            return new PlannerExerciseCandidate(
                CreateGuid(id),
                code,
                code,
                primaryMuscleGroup,
                [],
                movementPattern,
                exerciseType,
                isActive,
                equipment ?? []);
        }

        private static void AssertRequiredPatterns(
            RoutineSessionExerciseSelection session,
            params PatronMovimiento[] expectedPatterns)
        {
            var selectedRequiredPatterns = session.Slots
                .Where(slot => slot.IsRequired)
                .Select(slot => slot.Exercise!.MovementPattern)
                .ToArray();

            foreach (var pattern in expectedPatterns)
            {
                Assert.Contains(pattern, selectedRequiredPatterns);
            }
        }

        private static void AssertRequiredPatternFamily(
            RoutineSessionExerciseSelection session,
            params PatronMovimiento[] acceptedPatterns)
        {
            Assert.Contains(
                session.Slots.Where(slot => slot.IsRequired),
                slot => slot.Exercise is not null &&
                    acceptedPatterns.Contains(
                        slot.Exercise.MovementPattern));
        }

        private static Guid CreateGuid(int value)
        {
            return Guid.Parse($"00000000-0000-0000-0000-{value:D12}");
        }
    }
}
