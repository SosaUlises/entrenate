using Entrenate.Application.Coach.AdaptiveTraining.Lighter;
using Entrenate.Application.Coach.AdaptiveTraining.TimeAdaptation;
using Entrenate.Domain.Enums;
using Xunit;

namespace Entrenate.Application.UnitTests.Coach.AdaptiveTraining.Lighter
{
    public class AdaptiveTrainingLighterEngineTests
    {
        [Theory]
        [InlineData(0, 1, 1, 2)]
        [InlineData(1, 2, 2, 3)]
        [InlineData(2, 3, 3, 4)]
        [InlineData(3, 4, 4, 5)]
        public void Adapt_IncrementsRirByOne(
            int inputMinRir,
            int inputMaxRir,
            int expectedMinRir,
            int expectedMaxRir)
        {
            var exercise = CreateExercise(
                id: 1,
                order: 1,
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                sets: 3,
                minRir: inputMinRir,
                maxRir: inputMaxRir);

            var session = CreateSession(exercise);

            var result = AdaptiveTrainingLighterEngine.Adapt(session);

            Assert.Equal(
                AdaptiveTrainingLighterFeasibility.FeasibleWithChanges,
                result.Feasibility);

            var adapted = Assert.Single(result.AdaptedSession.Exercises);
            Assert.Equal(expectedMinRir, adapted.Prescription.MinRir);
            Assert.Equal(expectedMaxRir, adapted.Prescription.MaxRir);

            var change = Assert.Single(result.Changes);
            Assert.Equal(
                AdaptiveTrainingLighterChangeType.RirIncreased,
                change.Type);
            Assert.Equal(inputMinRir, change.OriginalMinRir);
            Assert.Equal(inputMaxRir, change.OriginalMaxRir);
            Assert.Equal(expectedMinRir, change.AdaptedMinRir);
            Assert.Equal(expectedMaxRir, change.AdaptedMaxRir);

            var diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal(
                AdaptiveTrainingLighterDiagnosticCode.RirIncreased,
                diagnostic.Code);
        }

        [Theory]
        [InlineData(4, 5, 5, 5)]
        [InlineData(5, 5, 5, 5)]
        public void Adapt_CapsRirAtFive(
            int inputMinRir,
            int inputMaxRir,
            int expectedMinRir,
            int expectedMaxRir)
        {
            var exercise = CreateExercise(
                id: 1,
                order: 1,
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                sets: 3,
                minRir: inputMinRir,
                maxRir: inputMaxRir);

            var session = CreateSession(exercise);

            var result = AdaptiveTrainingLighterEngine.Adapt(session);

            var adapted = Assert.Single(result.AdaptedSession.Exercises);
            Assert.Equal(expectedMinRir, adapted.Prescription.MinRir);
            Assert.Equal(expectedMaxRir, adapted.Prescription.MaxRir);

            if (inputMinRir == 5 && inputMaxRir == 5)
            {
                // RIR was already capped at 5 and compound doesn't reduce sets -> neutral
                Assert.Equal(
                    AdaptiveTrainingLighterFeasibility.Feasible,
                    result.Feasibility);
                Assert.Empty(result.Changes);
                var diagnostic = Assert.Single(result.Diagnostics);
                Assert.Equal(
                    AdaptiveTrainingLighterDiagnosticCode.NeutralLighterTransformation,
                    diagnostic.Code);
            }
            else
            {
                Assert.Equal(
                    AdaptiveTrainingLighterFeasibility.FeasibleWithChanges,
                    result.Feasibility);
            }
        }

        [Fact]
        public void Adapt_PreservesValidRirRange_AndRejectsInvalidInput()
        {
            var invalidExercise = CreateExercise(
                id: 1,
                order: 1,
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                sets: 3,
                minRir: 4,
                maxRir: 2); // Invalid: MinRir > MaxRir

            var session = CreateSession(invalidExercise);

            Assert.Throws<ArgumentException>(() =>
                AdaptiveTrainingLighterEngine.Adapt(session));
        }

        [Fact]
        public void Adapt_PreservesNullRir_DoesNotInventRir()
        {
            var exerciseWithNullRir = CreateExercise(
                id: 1,
                order: 1,
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                sets: 3,
                minRir: null,
                maxRir: null);

            var session = CreateSession(exerciseWithNullRir);

            var result = AdaptiveTrainingLighterEngine.Adapt(session);

            var adapted = Assert.Single(result.AdaptedSession.Exercises);
            Assert.Null(adapted.Prescription.MinRir);
            Assert.Null(adapted.Prescription.MaxRir);
            // No sets reduced and RIR was null -> neutral
            Assert.Equal(
                AdaptiveTrainingLighterFeasibility.Feasible,
                result.Feasibility);
            Assert.Empty(result.Changes);
        }

        [Fact]
        public void Adapt_WhenOptionalAccessoryHasMultipleSets_ReducesOneSet()
        {
            var optionalAccessory = CreateExercise(
                id: 1,
                order: 1,
                type: TipoEjercicio.Aislamiento,
                pattern: PatronMovimiento.FlexionCodo,
                primaryMuscle: "Bíceps",
                sets: 4,
                isRequiredIntent: false,
                minRir: 1,
                maxRir: 2);

            var session = CreateSession(optionalAccessory);

            var result = AdaptiveTrainingLighterEngine.Adapt(session);

            var adapted = Assert.Single(result.AdaptedSession.Exercises);
            Assert.Equal(3, adapted.Prescription.Sets); // 4 -> 3
            Assert.Equal(2, adapted.Prescription.MinRir);
            Assert.Equal(3, adapted.Prescription.MaxRir);

            Assert.Equal(2, result.Changes.Count);
            Assert.Contains(
                result.Changes,
                c => c.Type == AdaptiveTrainingLighterChangeType.RirIncreased);
            var setsChange = Assert.Single(
                result.Changes,
                c => c.Type == AdaptiveTrainingLighterChangeType.OptionalAccessorySetsReduced);
            Assert.Equal(4, setsChange.OriginalSets);
            Assert.Equal(3, setsChange.AdaptedSets);
        }

        [Fact]
        public void Adapt_WhenOptionalAccessoryHasOneSet_PreservesSetAndDoesNotEliminate()
        {
            var optionalAccessory = CreateExercise(
                id: 1,
                order: 1,
                type: TipoEjercicio.Aislamiento,
                pattern: PatronMovimiento.FlexionCodo,
                primaryMuscle: "Bíceps",
                sets: 1,
                isRequiredIntent: false,
                minRir: 1,
                maxRir: 2);

            var session = CreateSession(optionalAccessory);

            var result = AdaptiveTrainingLighterEngine.Adapt(session);

            var adapted = Assert.Single(result.AdaptedSession.Exercises);
            Assert.Equal(1, adapted.Prescription.Sets); // Kept at 1, not reduced to 0
            Assert.Equal(2, adapted.Prescription.MinRir);
            Assert.Equal(3, adapted.Prescription.MaxRir);

            // Only RIR increased, no sets reduced
            var change = Assert.Single(result.Changes);
            Assert.Equal(
                AdaptiveTrainingLighterChangeType.RirIncreased,
                change.Type);
        }

        [Fact]
        public void Adapt_RequiredExercisesDoNotReduceSets()
        {
            var compound = CreateExercise(
                id: 1,
                order: 1,
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                sets: 4);

            var requiredIsolation = CreateExercise(
                id: 2,
                order: 2,
                type: TipoEjercicio.Aislamiento,
                pattern: PatronMovimiento.ExtensionCodo,
                primaryMuscle: "Tríceps",
                sets: 3,
                isRequiredIntent: true);

            var unclassifiedRequired = CreateExercise(
                id: 3,
                order: 3,
                type: null,
                pattern: null,
                primaryMuscle: "Core",
                sets: 3);

            var session = CreateSession(
                compound,
                requiredIsolation,
                unclassifiedRequired);

            var result = AdaptiveTrainingLighterEngine.Adapt(session);

            Assert.Equal(3, result.AdaptedSession.Exercises.Count);
            Assert.Equal(4, result.AdaptedSession.Exercises[0].Prescription.Sets);
            Assert.Equal(3, result.AdaptedSession.Exercises[1].Prescription.Sets);
            Assert.Equal(3, result.AdaptedSession.Exercises[2].Prescription.Sets);
            Assert.DoesNotContain(
                result.Changes,
                c => c.Type == AdaptiveTrainingLighterChangeType.OptionalAccessorySetsReduced);
        }

        [Fact]
        public void Adapt_PreservesRepsAndRestSeconds()
        {
            var customPrescription = new AdaptiveTrainingLighterPrescription(
                Sets: 3,
                MinReps: 6,
                MaxReps: 10,
                MinRir: 2,
                MaxRir: 3,
                RestSeconds: 150,
                Notes: "Cuidar técnica");

            var exercise = new PlannedLighterExercise(
                Guid.NewGuid(),
                Id(1),
                "bench",
                "Press de banca",
                1,
                PatronMovimiento.EmpujeHorizontal,
                TipoEjercicio.Compuesto,
                "Pecho",
                ["Tríceps"],
                [Equip(1)],
                null,
                customPrescription);

            var session = CreateSession(exercise);

            var result = AdaptiveTrainingLighterEngine.Adapt(session);

            var adapted = Assert.Single(result.AdaptedSession.Exercises);
            Assert.Equal(6, adapted.Prescription.MinReps);
            Assert.Equal(10, adapted.Prescription.MaxReps);
            Assert.Equal(150, adapted.Prescription.RestSeconds);
            Assert.Equal("Cuidar técnica", adapted.Prescription.Notes);
        }

        [Fact]
        public void Adapt_PreservesExerciseIdentityAndOrder()
        {
            var ex1 = CreateExercise(
                id: 1,
                order: 1,
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho");

            var ex2 = CreateExercise(
                id: 2,
                order: 2,
                type: TipoEjercicio.Aislamiento,
                pattern: PatronMovimiento.ExtensionCodo,
                primaryMuscle: "Tríceps",
                isRequiredIntent: false);

            var session = CreateSession(ex1, ex2);

            var result = AdaptiveTrainingLighterEngine.Adapt(session);

            Assert.Equal(2, result.AdaptedSession.Exercises.Count);
            Assert.Equal(ex1.SourceRoutineExerciseId, result.AdaptedSession.Exercises[0].SourceRoutineExerciseId);
            Assert.Equal(ex1.ExerciseId, result.AdaptedSession.Exercises[0].ExerciseId);
            Assert.Equal(ex1.Order, result.AdaptedSession.Exercises[0].Order);

            Assert.Equal(ex2.SourceRoutineExerciseId, result.AdaptedSession.Exercises[1].SourceRoutineExerciseId);
            Assert.Equal(ex2.ExerciseId, result.AdaptedSession.Exercises[1].ExerciseId);
            Assert.Equal(ex2.Order, result.AdaptedSession.Exercises[1].Order);
        }

        [Fact]
        public void Adapt_RecalculatesDurationAccurately()
        {
            // Compound: 3 sets, 120 rest
            // Set time: 45 * 3 + 120 * 2 = 135 + 240 = 375 s
            var compound = CreateExercise(
                id: 1,
                order: 1,
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                sets: 3,
                restSeconds: 120);

            // Optional accessory: 3 sets, 60 rest
            // Original set time: 45 * 3 + 60 * 2 = 135 + 120 = 255 s
            // Total original = 300 (prep) + 60 (1 transition) + 375 + 255 = 990 s
            var accessory = CreateExercise(
                id: 2,
                order: 2,
                type: TipoEjercicio.Aislamiento,
                pattern: PatronMovimiento.FlexionCodo,
                primaryMuscle: "Bíceps",
                sets: 3,
                restSeconds: 60,
                isRequiredIntent: false);

            var session = CreateSession(compound, accessory);

            var result = AdaptiveTrainingLighterEngine.Adapt(session);

            // Adapted accessory: 2 sets, 60 rest
            // Adapted set time: 45 * 2 + 60 * 1 = 90 + 60 = 150 s
            // Total adapted = 300 + 60 + 375 + 150 = 885 s (reduction of 105 s = 45s set + 60s rest)
            Assert.Equal(990, result.OriginalSession.EstimatedDurationSeconds);
            Assert.Equal(885, result.AdaptedSession.EstimatedDurationSeconds);
        }

        [Fact]
        public void Adapt_ProducesIdenticalResultForSameInput()
        {
            var ex1 = CreateExercise(
                id: 1,
                order: 1,
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                sets: 4,
                minRir: 1,
                maxRir: 2);

            var ex2 = CreateExercise(
                id: 2,
                order: 2,
                type: TipoEjercicio.Aislamiento,
                pattern: PatronMovimiento.ExtensionCodo,
                primaryMuscle: "Tríceps",
                sets: 3,
                minRir: 2,
                maxRir: 3,
                isRequiredIntent: false);

            var session = CreateSession(ex1, ex2);

            var resultA = AdaptiveTrainingLighterEngine.Adapt(session);
            var resultB = AdaptiveTrainingLighterEngine.Adapt(session);

            Assert.Equal(
                resultA.AdaptedSession.EstimatedDurationSeconds,
                resultB.AdaptedSession.EstimatedDurationSeconds);
            Assert.Equal(resultA.Feasibility, resultB.Feasibility);
            Assert.Equal(resultA.Changes.Count, resultB.Changes.Count);
            Assert.Equal(
                resultA.AdaptedSession.Exercises[0].Prescription.MinRir,
                resultB.AdaptedSession.Exercises[0].Prescription.MinRir);
            Assert.Equal(
                resultA.AdaptedSession.Exercises[1].Prescription.Sets,
                resultB.AdaptedSession.Exercises[1].Prescription.Sets);
        }

        private static PlannedLighterSession CreateSession(
            params PlannedLighterExercise[] exercises)
        {
            return new PlannedLighterSession(
                Guid.NewGuid(),
                exercises);
        }

        private static PlannedLighterExercise CreateExercise(
            int id,
            int order,
            TipoEjercicio? type,
            PatronMovimiento? pattern,
            string primaryMuscle,
            int sets = 3,
            int? minRir = 1,
            int? maxRir = 2,
            int restSeconds = 90,
            bool? isRequiredIntent = null)
        {
            return new PlannedLighterExercise(
                Guid.NewGuid(),
                Id(id),
                $"ex_{id}",
                $"Exercise {id}",
                order,
                pattern,
                type,
                primaryMuscle,
                ["Secundario"],
                [Equip(1)],
                isRequiredIntent,
                new AdaptiveTrainingLighterPrescription(
                    Sets: sets,
                    MinReps: 8,
                    MaxReps: 12,
                    MinRir: minRir,
                    MaxRir: maxRir,
                    RestSeconds: restSeconds,
                    Notes: null));
        }

        private static Guid Id(int value) =>
            new($"00000000-0000-0000-0000-{value:D12}");

        private static Guid Equip(int value) =>
            new($"00000000-0000-0000-0001-{value:D12}");
    }
}

