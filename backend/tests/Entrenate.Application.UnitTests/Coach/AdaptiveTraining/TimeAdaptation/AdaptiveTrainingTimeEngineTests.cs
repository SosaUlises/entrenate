using Entrenate.Application.Coach.AdaptiveTraining.TimeAdaptation;
using Entrenate.Domain.Enums;
using Xunit;

namespace Entrenate.Application.UnitTests.Coach.AdaptiveTraining.TimeAdaptation
{
    public class AdaptiveTrainingTimeEngineTests
    {
        [Fact]
        public void Adapt_CalculatesNormativeDuration()
        {
            var session = Session(
                Exercise(
                    1,
                    1,
                    TipoEjercicio.Compuesto,
                    PatronMovimiento.EmpujeHorizontal,
                    "Pecho",
                    sets: 3,
                    restSeconds: 120),
                Exercise(
                    2,
                    2,
                    TipoEjercicio.Aislamiento,
                    PatronMovimiento.ExtensionCodo,
                    "Tríceps",
                    sets: 2,
                    restSeconds: 60,
                    requiredIntent: true));

            var result = AdaptiveTrainingTimeEngine.Adapt(session, 15);

            Assert.Equal(885, result.OriginalSession.EstimatedDurationSeconds);
            Assert.Equal(885, result.AdaptedSession.EstimatedDurationSeconds);
        }

        [Fact]
        public void Adapt_WhenOriginalFits_ReturnsWithoutChanges()
        {
            var session = Session(
                Exercise(
                    1,
                    1,
                    TipoEjercicio.Compuesto,
                    PatronMovimiento.DominanteRodilla,
                    "Cuádriceps",
                    sets: 3,
                    restSeconds: 60));

            var result = AdaptiveTrainingTimeEngine.Adapt(session, 15);

            Assert.Equal(
                AdaptiveTrainingTimeFeasibility.Feasible,
                result.Feasibility);
            Assert.Empty(result.Changes);
            Assert.Empty(result.Diagnostics);
            Assert.Equal(
                result.OriginalSession,
                result.AdaptedSession);
        }

        [Fact]
        public void Adapt_RemovesLowestPriorityOptionalAccessoryFirst()
        {
            var session = Session(
                Exercise(
                    1,
                    1,
                    TipoEjercicio.Compuesto,
                    PatronMovimiento.EmpujeHorizontal,
                    "Pecho",
                    sets: 2,
                    restSeconds: 180),
                Exercise(
                    2,
                    2,
                    TipoEjercicio.Aislamiento,
                    PatronMovimiento.FlexionCodo,
                    "Bíceps",
                    sets: 4,
                    restSeconds: 180,
                    requiredIntent: false),
                Exercise(
                    3,
                    3,
                    TipoEjercicio.Aislamiento,
                    PatronMovimiento.ExtensionCodo,
                    "Tríceps",
                    sets: 4,
                    restSeconds: 180,
                    requiredIntent: false));

            var result = AdaptiveTrainingTimeEngine.Adapt(session, 23);

            var change = Assert.Single(result.Changes);
            Assert.Equal(
                AdaptiveTrainingTimeChangeType.OptionalAccessoryRemoved,
                change.Type);
            Assert.Equal(Id(3), change.ExerciseId);
            Assert.DoesNotContain(
                result.AdaptedSession.Exercises,
                exercise => exercise.ExerciseId == Id(3));
        }

        [Fact]
        public void Adapt_ReducesRemainingOptionalAccessoryToFit()
        {
            var session = Session(
                Exercise(
                    1,
                    1,
                    TipoEjercicio.Compuesto,
                    PatronMovimiento.EmpujeHorizontal,
                    "Pecho",
                    sets: 2,
                    restSeconds: 180),
                Exercise(
                    2,
                    2,
                    TipoEjercicio.Aislamiento,
                    PatronMovimiento.ExtensionCodo,
                    "Tríceps",
                    sets: 4,
                    restSeconds: 180,
                    requiredIntent: false));

            var result = AdaptiveTrainingTimeEngine.Adapt(session, 20);

            var change = Assert.Single(result.Changes);
            Assert.Equal(
                AdaptiveTrainingTimeChangeType.ExerciseSetsReduced,
                change.Type);
            Assert.Equal(4, change.OriginalSets);
            Assert.Equal(3, change.AdaptedSets);
            Assert.Equal(
                AdaptiveTrainingTimeFeasibility.FeasibleWithChanges,
                result.Feasibility);
        }

        [Fact]
        public void Adapt_ReducesRequiredIsolationAfterOptionalWork()
        {
            var session = Session(
                Exercise(
                    1,
                    1,
                    TipoEjercicio.Compuesto,
                    PatronMovimiento.EmpujeHorizontal,
                    "Pecho",
                    sets: 2,
                    restSeconds: 180),
                Exercise(
                    2,
                    2,
                    TipoEjercicio.Aislamiento,
                    PatronMovimiento.ExtensionCodo,
                    "Tríceps",
                    sets: 4,
                    restSeconds: 180,
                    requiredIntent: true));

            var result = AdaptiveTrainingTimeEngine.Adapt(session, 20);

            var adapted = result.AdaptedSession.Exercises.Single(
                exercise => exercise.ExerciseId == Id(2));
            Assert.Equal(
                AdaptiveTrainingExercisePriority.RequiredIsolation,
                adapted.Priority);
            Assert.Equal(3, adapted.Prescription.Sets);
        }

        [Fact]
        public void Adapt_ReducesRequiredCompoundToItsFloor()
        {
            var session = Session(
                Exercise(
                    1,
                    1,
                    TipoEjercicio.Compuesto,
                    PatronMovimiento.BisagraCadera,
                    "Isquiotibiales",
                    sets: 4,
                    restSeconds: 180));

            var result = AdaptiveTrainingTimeEngine.Adapt(session, 15);

            var adapted = Assert.Single(result.AdaptedSession.Exercises);
            Assert.Equal(3, adapted.Prescription.Sets);
            Assert.Equal(795, result.AdaptedSession.EstimatedDurationSeconds);
        }

        [Fact]
        public void Adapt_UsesOriginalSetsWhenBelowGeneralFloor()
        {
            var session = Session(
                Exercise(
                    1,
                    1,
                    TipoEjercicio.Compuesto,
                    PatronMovimiento.EmpujeHorizontal,
                    "Pecho",
                    sets: 1,
                    restSeconds: 900),
                Exercise(
                    2,
                    2,
                    TipoEjercicio.Compuesto,
                    PatronMovimiento.TraccionHorizontal,
                    "Espalda",
                    sets: 3,
                    restSeconds: 300));

            var result = AdaptiveTrainingTimeEngine.Adapt(session, 15);

            var belowGeneralFloor = result.AdaptedSession.Exercises.Single(
                exercise => exercise.ExerciseId == Id(1));
            Assert.Equal(1, belowGeneralFloor.Prescription.Sets);
            Assert.Equal(
                AdaptiveTrainingTimeFeasibility.FeasibleWithChanges,
                result.Feasibility);
        }

        [Fact]
        public void Adapt_PreservesRestRepsAndRirWhenReducingSets()
        {
            var session = Session(
                Exercise(
                    1,
                    1,
                    TipoEjercicio.Compuesto,
                    PatronMovimiento.BisagraCadera,
                    "Isquiotibiales",
                    sets: 4,
                    restSeconds: 180,
                    minReps: 6,
                    maxReps: 8,
                    minRir: 1,
                    maxRir: 2));

            var result = AdaptiveTrainingTimeEngine.Adapt(session, 15);

            var original = Assert.Single(result.OriginalSession.Exercises);
            var adapted = Assert.Single(result.AdaptedSession.Exercises);
            Assert.Equal(
                original.Prescription.RestSeconds,
                adapted.Prescription.RestSeconds);
            Assert.Equal(original.Prescription.MinReps,
                adapted.Prescription.MinReps);
            Assert.Equal(original.Prescription.MaxReps,
                adapted.Prescription.MaxReps);
            Assert.Equal(original.Prescription.MinRir,
                adapted.Prescription.MinRir);
            Assert.Equal(original.Prescription.MaxRir,
                adapted.Prescription.MaxRir);
        }

        [Fact]
        public void Adapt_WhenMinimumStillExceedsTime_ReturnsNotFeasible()
        {
            var session = Session(
                Exercise(
                    1,
                    1,
                    TipoEjercicio.Compuesto,
                    PatronMovimiento.EmpujeHorizontal,
                    "Pecho",
                    sets: 3,
                    restSeconds: 900),
                Exercise(
                    2,
                    2,
                    TipoEjercicio.Compuesto,
                    PatronMovimiento.TraccionHorizontal,
                    "Espalda",
                    sets: 3,
                    restSeconds: 900));

            var result = AdaptiveTrainingTimeEngine.Adapt(session, 15);

            Assert.Equal(
                AdaptiveTrainingTimeFeasibility.NotFeasible,
                result.Feasibility);
            Assert.All(
                result.AdaptedSession.Exercises,
                exercise => Assert.Equal(2, exercise.Prescription.Sets));
            Assert.True(
                result.AdaptedSession.EstimatedDurationSeconds > 15 * 60);
            Assert.Contains(
                result.Diagnostics,
                diagnostic => diagnostic.Code ==
                    AdaptiveTrainingTimeDiagnosticCode
                        .MinimumSessionExceedsAvailableTime);
        }

        [Fact]
        public void Adapt_FallbackClassifiesUniqueIsolationAsRequired()
        {
            var session = Session(
                Exercise(
                    1,
                    1,
                    TipoEjercicio.Aislamiento,
                    PatronMovimiento.CoreAntiExtension,
                    "Core",
                    sets: 2),
                Exercise(
                    2,
                    2,
                    TipoEjercicio.Aislamiento,
                    PatronMovimiento.FlexionCodo,
                    "Bíceps",
                    sets: 2),
                Exercise(
                    3,
                    3,
                    TipoEjercicio.Aislamiento,
                    PatronMovimiento.FlexionCodo,
                    "Bíceps",
                    sets: 2));

            var result = AdaptiveTrainingTimeEngine.Adapt(session, 60);

            Assert.Equal(
                AdaptiveTrainingExercisePriority.RequiredIsolation,
                result.OriginalSession.Exercises[0].Priority);
            Assert.All(
                result.OriginalSession.Exercises.Skip(1),
                exercise => Assert.Equal(
                    AdaptiveTrainingExercisePriority.OptionalAccessory,
                    exercise.Priority));
        }

        [Fact]
        public void Adapt_ExplicitIsolationIntentOverridesFallback()
        {
            var session = Session(
                Exercise(
                    1,
                    1,
                    TipoEjercicio.Aislamiento,
                    PatronMovimiento.CoreFlexion,
                    "Core",
                    sets: 2,
                    requiredIntent: false),
                Exercise(
                    2,
                    2,
                    TipoEjercicio.Aislamiento,
                    PatronMovimiento.FlexionCodo,
                    "Bíceps",
                    sets: 2,
                    requiredIntent: true),
                Exercise(
                    3,
                    3,
                    TipoEjercicio.Aislamiento,
                    PatronMovimiento.FlexionCodo,
                    "Bíceps",
                    sets: 2,
                    requiredIntent: false));

            var result = AdaptiveTrainingTimeEngine.Adapt(session, 60);

            Assert.Equal(
                AdaptiveTrainingExercisePriority.OptionalAccessory,
                result.OriginalSession.Exercises[0].Priority);
            Assert.Equal(
                AdaptiveTrainingExercisePriority.RequiredIsolation,
                result.OriginalSession.Exercises[1].Priority);
            Assert.Equal(
                AdaptiveTrainingExercisePriority.OptionalAccessory,
                result.OriginalSession.Exercises[2].Priority);
        }

        [Fact]
        public void Adapt_IsDeterministicRegardlessOfInputCollectionOrder()
        {
            var first = Exercise(
                1,
                1,
                TipoEjercicio.Compuesto,
                PatronMovimiento.EmpujeHorizontal,
                "Pecho",
                sets: 3,
                restSeconds: 180);
            var second = Exercise(
                2,
                2,
                TipoEjercicio.Aislamiento,
                PatronMovimiento.ExtensionCodo,
                "Tríceps",
                sets: 4,
                restSeconds: 180,
                requiredIntent: false);

            var ordered = AdaptiveTrainingTimeEngine.Adapt(
                Session(first, second),
                18);
            var reversed = AdaptiveTrainingTimeEngine.Adapt(
                Session(second, first),
                18);

            Assert.Equal(ordered.PolicyVersion, reversed.PolicyVersion);
            Assert.Equal(ordered.Feasibility, reversed.Feasibility);
            Assert.Equal(
                ordered.OriginalSession.EstimatedDurationSeconds,
                reversed.OriginalSession.EstimatedDurationSeconds);
            Assert.Equal(
                ordered.AdaptedSession.EstimatedDurationSeconds,
                reversed.AdaptedSession.EstimatedDurationSeconds);
            Assert.Equal(
                ordered.AdaptedSession.Exercises,
                reversed.AdaptedSession.Exercises);
            Assert.Equal(ordered.Changes, reversed.Changes);
            Assert.Equal(ordered.Diagnostics, reversed.Diagnostics);
        }

        private static PlannedAdaptiveTrainingSession Session(
            params PlannedAdaptiveTrainingExercise[] exercises)
        {
            return new PlannedAdaptiveTrainingSession(Id(999), exercises);
        }

        private static PlannedAdaptiveTrainingExercise Exercise(
            int id,
            int order,
            TipoEjercicio? type,
            PatronMovimiento? pattern,
            string primaryMuscle,
            int sets,
            int restSeconds = 60,
            bool? requiredIntent = null,
            int minReps = 8,
            int maxReps = 12,
            int minRir = 1,
            int maxRir = 3)
        {
            return new PlannedAdaptiveTrainingExercise(
                Id(1000 + id),
                Id(id),
                $"exercise-{id:D2}",
                $"Exercise {id}",
                order,
                pattern,
                type,
                primaryMuscle,
                requiredIntent,
                new AdaptiveTrainingPrescription(
                    sets,
                    minReps,
                    maxReps,
                    minRir,
                    maxRir,
                    restSeconds,
                    Notes: null));
        }

        private static Guid Id(int value)
        {
            return Guid.Parse(
                $"00000000-0000-0000-0000-{value:D12}");
        }
    }
}
