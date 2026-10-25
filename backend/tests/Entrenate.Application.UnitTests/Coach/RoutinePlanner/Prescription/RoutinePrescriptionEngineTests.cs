using Entrenate.Application.Coach.RoutinePlanner.ExerciseSelection;
using Entrenate.Application.Coach.RoutinePlanner.Prescription;
using Entrenate.Application.Coach.RoutinePlanner.WeeklyStructure;
using Entrenate.Domain.Enums;
using Xunit;

namespace Entrenate.Application.UnitTests.Coach.RoutinePlanner.Prescription
{
    public class RoutinePrescriptionEngineTests
    {
        [Theory]
        [InlineData(
            ObjetivoEntrenamiento.GanarFuerza,
            NivelExperiencia.Principiante,
            3, 4, 6, 2, 3, 180)]
        [InlineData(
            ObjetivoEntrenamiento.GanarMasaMuscular,
            NivelExperiencia.Avanzado,
            4, 6, 12, 1, 3, 120)]
        [InlineData(
            ObjetivoEntrenamiento.GanarmasaMuscularYFuerza,
            NivelExperiencia.Intermedio,
            3, 5, 8, 2, 3, 150)]
        [InlineData(
            ObjetivoEntrenamiento.AcondicionamientoGeneral,
            NivelExperiencia.SinExperiencia,
            2, 8, 12, 3, 4, 90)]
        public void Prescribe_AppliesGoalAndExperiencePolicy(
            ObjetivoEntrenamiento goal,
            NivelExperiencia experience,
            int expectedSets,
            int expectedMinReps,
            int expectedMaxReps,
            int expectedMinRir,
            int expectedMaxRir,
            int expectedRestSeconds)
        {
            var selection = Selection(
                Session(
                    Slot(
                        1,
                        ExerciseSlotRole.PrimaryCompound,
                        true,
                        Exercise(
                            1,
                            "squat",
                            "Cuádriceps",
                            PatronMovimiento.DominanteRodilla,
                            TipoEjercicio.Compuesto))));

            var result = RoutinePrescriptionEngine.Prescribe(
                selection,
                Profile(goal, experience, 60));
            var exercise = result.Sessions[0].Exercises[0];

            Assert.Equal(expectedSets, exercise.Sets);
            Assert.Equal(expectedMinReps, exercise.MinReps);
            Assert.Equal(expectedMaxReps, exercise.MaxReps);
            Assert.Equal(expectedMinRir, exercise.MinRir);
            Assert.Equal(expectedMaxRir, exercise.MaxRir);
            Assert.Equal(expectedRestSeconds, exercise.RestSeconds);
        }

        [Fact]
        public void Prescribe_DifferentiatesSecondaryCompoundAndIsolation()
        {
            var selection = Selection(
                Session(
                    Slot(
                        1,
                        ExerciseSlotRole.SecondaryCompound,
                        true,
                        Exercise(
                            1,
                            "press",
                            "Hombros",
                            PatronMovimiento.EmpujeVertical,
                            TipoEjercicio.Compuesto)),
                    Slot(
                        2,
                        ExerciseSlotRole.Accessory,
                        false,
                        Exercise(
                            2,
                            "triceps",
                            "Tríceps",
                            PatronMovimiento.ExtensionCodo,
                            TipoEjercicio.Aislamiento))));

            var result = RoutinePrescriptionEngine.Prescribe(
                selection,
                Profile(
                    ObjetivoEntrenamiento.GanarFuerza,
                    NivelExperiencia.SinExperiencia,
                    60));

            Assert.Equal(120, result.Sessions[0].Exercises[0].RestSeconds);
            Assert.Equal(90, result.Sessions[0].Exercises[1].RestSeconds);
            Assert.All(
                result.Sessions[0].Exercises,
                exercise =>
                {
                    Assert.Equal(6, exercise.MinReps);
                    Assert.Equal(10, exercise.MaxReps);
                });
        }

        [Fact]
        public void Prescribe_CalculatesPrimaryAndSecondaryEffectiveVolume()
        {
            var selection = Selection(
                Session(
                    Slot(
                        1,
                        ExerciseSlotRole.PrimaryCompound,
                        true,
                        Exercise(
                            1,
                            "bench",
                            "Pecho",
                            PatronMovimiento.EmpujeHorizontal,
                            TipoEjercicio.Compuesto,
                            ["Tríceps"])),
                    Slot(
                        2,
                        ExerciseSlotRole.Accessory,
                        false,
                        Exercise(
                            2,
                            "triceps",
                            "Tríceps",
                            PatronMovimiento.ExtensionCodo,
                            TipoEjercicio.Aislamiento,
                            ["Hombros"]))));

            var result = RoutinePrescriptionEngine.Prescribe(
                selection,
                Profile(
                    ObjetivoEntrenamiento.GanarFuerza,
                    NivelExperiencia.SinExperiencia,
                    60));
            var volumes = result.WeeklyMuscleVolumes.ToDictionary(
                volume => volume.MuscleGroup,
                volume => volume.EffectiveSets);

            Assert.Equal(2m, volumes["Pecho"]);
            Assert.Equal(3m, volumes["Tríceps"]);
            Assert.DoesNotContain("Hombros", volumes.Keys);
        }

        [Fact]
        public void Prescribe_DistributesWeeklySetsWithAtMostOneSetDifference()
        {
            var selection = Selection(
                Session(Slot(1, ExerciseSlotRole.PrimaryCompound, true,
                    Exercise(1, "squat-a", "Cuádriceps",
                        PatronMovimiento.DominanteRodilla,
                        TipoEjercicio.Compuesto)), 1),
                Session(Slot(1, ExerciseSlotRole.PrimaryCompound, true,
                    Exercise(2, "squat-b", "Cuádriceps",
                        PatronMovimiento.DominanteRodilla,
                        TipoEjercicio.Compuesto)), 2),
                Session(Slot(1, ExerciseSlotRole.PrimaryCompound, true,
                    Exercise(3, "squat-c", "Cuádriceps",
                        PatronMovimiento.DominanteRodilla,
                        TipoEjercicio.Compuesto)), 3));

            var result = RoutinePrescriptionEngine.Prescribe(
                selection,
                Profile(
                    ObjetivoEntrenamiento.GanarmasaMuscularYFuerza,
                    NivelExperiencia.Principiante,
                    60));
            var sets = result.Sessions
                .SelectMany(session => session.Exercises)
                .Select(exercise => exercise.Sets)
                .ToArray();

            Assert.Equal(7, sets.Sum());
            Assert.True(sets.Max() - sets.Min() <= 1);
        }

        [Fact]
        public void Prescribe_CalculatesDurationAndKeepsSessionWithinTolerance()
        {
            var selection = Selection(
                Session(
                    Slot(
                        1,
                        ExerciseSlotRole.PrimaryCompound,
                        true,
                        Exercise(
                            1,
                            "squat",
                            "Cuádriceps",
                            PatronMovimiento.DominanteRodilla,
                            TipoEjercicio.Compuesto)),
                    Slot(
                        2,
                        ExerciseSlotRole.PrimaryCompound,
                        true,
                        Exercise(
                            2,
                            "row",
                            "Espalda",
                            PatronMovimiento.TraccionHorizontal,
                            TipoEjercicio.Compuesto))));

            var result = RoutinePrescriptionEngine.Prescribe(
                selection,
                Profile(
                    ObjetivoEntrenamiento.GanarFuerza,
                    NivelExperiencia.SinExperiencia,
                    10 + 5));

            Assert.Equal(900, result.Sessions[0].EstimatedDurationSeconds);
            Assert.DoesNotContain(
                result.PrescriptionDiagnostics,
                diagnostic => diagnostic.Code is
                    RoutinePrescriptionDiagnosticCode
                        .OptionalExerciseRemovedForDuration or
                    RoutinePrescriptionDiagnosticCode
                        .ExerciseSetsReducedForDuration or
                    RoutinePrescriptionDiagnosticCode
                        .SessionDurationExceeded);
        }

        [Fact]
        public void Prescribe_RemovesOptionalAccessoryBeforeReducingPrimary()
        {
            var selection = Selection(
                Session(
                    Slot(
                        1,
                        ExerciseSlotRole.PrimaryCompound,
                        true,
                        Exercise(
                            1,
                            "squat",
                            "Cuádriceps",
                            PatronMovimiento.DominanteRodilla,
                            TipoEjercicio.Compuesto)),
                    Slot(
                        2,
                        ExerciseSlotRole.Accessory,
                        false,
                        Exercise(
                            2,
                            "calf",
                            "Gemelos",
                            PatronMovimiento.FlexionPlantar,
                            TipoEjercicio.Aislamiento))));

            var result = RoutinePrescriptionEngine.Prescribe(
                selection,
                Profile(
                    ObjetivoEntrenamiento.GanarMasaMuscular,
                    NivelExperiencia.Avanzado,
                    15));

            Assert.Single(result.Sessions[0].Exercises);
            Assert.Equal(4, result.Sessions[0].Exercises[0].Sets);
            Assert.Contains(
                result.PrescriptionDiagnostics,
                diagnostic => diagnostic.Code ==
                    RoutinePrescriptionDiagnosticCode
                        .OptionalExerciseRemovedForDuration);
        }

        [Fact]
        public void Prescribe_ReducesSeriesWhenRequiredExerciseStillExceedsTime()
        {
            var selection = Selection(
                Session(
                    Slot(
                        1,
                        ExerciseSlotRole.PrimaryCompound,
                        true,
                        Exercise(
                            1,
                            "squat",
                            "Cuádriceps",
                            PatronMovimiento.DominanteRodilla,
                            TipoEjercicio.Compuesto)),
                    Slot(
                        2,
                        ExerciseSlotRole.Accessory,
                        true,
                        Exercise(
                            2,
                            "plank",
                            "Core",
                            PatronMovimiento.CoreAntiExtension,
                            TipoEjercicio.Aislamiento))));

            var result = RoutinePrescriptionEngine.Prescribe(
                selection,
                Profile(
                    ObjetivoEntrenamiento.GanarMasaMuscular,
                    NivelExperiencia.Avanzado,
                    15));

            Assert.Equal(3, result.Sessions[0].Exercises[0].Sets);
            Assert.True(
                result.Sessions[0].EstimatedDurationSeconds <= 15 * 60 * 1.1m);
            Assert.Contains(
                result.PrescriptionDiagnostics,
                diagnostic => diagnostic.Code ==
                    RoutinePrescriptionDiagnosticCode
                        .ExerciseSetsReducedForDuration);
        }

        [Fact]
        public void Prescribe_ReturnsNotFeasibleWhenMinimumSessionCannotFit()
        {
            var selection = Selection(
                Session(
                    Slot(1, ExerciseSlotRole.PrimaryCompound, true,
                        Exercise(1, "squat", "Cuádriceps",
                            PatronMovimiento.DominanteRodilla,
                            TipoEjercicio.Compuesto)),
                    Slot(2, ExerciseSlotRole.PrimaryCompound, true,
                        Exercise(2, "hinge", "Isquiotibiales",
                            PatronMovimiento.BisagraCadera,
                            TipoEjercicio.Compuesto)),
                    Slot(3, ExerciseSlotRole.PrimaryCompound, true,
                        Exercise(3, "bench", "Pecho",
                            PatronMovimiento.EmpujeHorizontal,
                            TipoEjercicio.Compuesto)),
                    Slot(4, ExerciseSlotRole.PrimaryCompound, true,
                        Exercise(4, "row", "Espalda",
                            PatronMovimiento.TraccionHorizontal,
                            TipoEjercicio.Compuesto)),
                    Slot(5, ExerciseSlotRole.Accessory, true,
                        Exercise(5, "plank", "Core",
                            PatronMovimiento.CoreAntiExtension,
                            TipoEjercicio.Aislamiento))));

            var result = RoutinePrescriptionEngine.Prescribe(
                selection,
                Profile(
                    ObjetivoEntrenamiento.GanarFuerza,
                    NivelExperiencia.SinExperiencia,
                    15));

            Assert.Equal(
                RoutinePrescriptionFeasibility.NotFeasible,
                result.Feasibility);
            Assert.Equal(5, result.Sessions[0].Exercises.Count);
            Assert.Contains(
                result.PrescriptionDiagnostics,
                diagnostic => diagnostic.Code ==
                    RoutinePrescriptionDiagnosticCode
                        .SessionDurationExceeded);
        }

        [Fact]
        public void Prescribe_IsDeterministicForSameInputs()
        {
            var selection = Selection(
                Session(
                    Slot(1, ExerciseSlotRole.PrimaryCompound, true,
                        Exercise(1, "bench", "Pecho",
                            PatronMovimiento.EmpujeHorizontal,
                            TipoEjercicio.Compuesto,
                            ["Tríceps"])),
                    Slot(2, ExerciseSlotRole.Accessory, false,
                        Exercise(2, "triceps", "Tríceps",
                            PatronMovimiento.ExtensionCodo,
                            TipoEjercicio.Aislamiento))));
            var profile = Profile(
                ObjetivoEntrenamiento.GanarMasaMuscular,
                NivelExperiencia.Principiante,
                30);

            var first = RoutinePrescriptionEngine.Prescribe(
                selection,
                profile);
            var second = RoutinePrescriptionEngine.Prescribe(
                selection,
                profile);

            Assert.Equal(
                first.Sessions.SelectMany(session => session.Exercises),
                second.Sessions.SelectMany(session => session.Exercises));
            Assert.Equal(
                first.WeeklyMuscleVolumes,
                second.WeeklyMuscleVolumes);
            Assert.Equal(
                first.PrescriptionDiagnostics,
                second.PrescriptionDiagnostics);
            Assert.Equal(first.Feasibility, second.Feasibility);
        }

        private static RoutinePrescriptionProfile Profile(
            ObjetivoEntrenamiento goal,
            NivelExperiencia experience,
            int durationMinutes)
        {
            return new RoutinePrescriptionProfile(
                goal,
                experience,
                durationMinutes);
        }

        private static WeeklyExerciseSelection Selection(
            params RoutineSessionExerciseSelection[] sessions)
        {
            return new WeeklyExerciseSelection(
                ExerciseSelectionFeasibility.Feasible,
                sessions,
                []);
        }

        private static RoutineSessionExerciseSelection Session(
            RoutineExerciseSlotSelection slot,
            int dayOrder = 1)
        {
            return Session([slot], dayOrder);
        }

        private static RoutineSessionExerciseSelection Session(
            params RoutineExerciseSlotSelection[] slots)
        {
            return Session(slots, 1);
        }

        private static RoutineSessionExerciseSelection Session(
            IReadOnlyList<RoutineExerciseSlotSelection> slots,
            int dayOrder)
        {
            return new RoutineSessionExerciseSelection(
                dayOrder,
                (DiaSemana)dayOrder,
                RoutineSessionFocus.FullBody,
                RoutineSessionVariant.None,
                slots);
        }

        private static RoutineExerciseSlotSelection Slot(
            int position,
            ExerciseSlotRole role,
            bool isRequired,
            SelectedRoutineExercise exercise)
        {
            return new RoutineExerciseSlotSelection(
                position,
                role,
                isRequired,
                [exercise.MovementPattern],
                exercise.ExerciseType,
                exercise);
        }

        private static SelectedRoutineExercise Exercise(
            int id,
            string code,
            string primaryMuscleGroup,
            PatronMovimiento movementPattern,
            TipoEjercicio exerciseType,
            IReadOnlyCollection<string>? secondaryMuscleGroups = null)
        {
            return new SelectedRoutineExercise(
                CreateGuid(id),
                code,
                code,
                primaryMuscleGroup,
                secondaryMuscleGroups ?? [],
                movementPattern,
                exerciseType);
        }

        private static Guid CreateGuid(int value)
        {
            return Guid.Parse($"00000000-0000-0000-0000-{value:D12}");
        }
    }
}
