using System.Reflection;
using Entrenate.Application.Coach.LiveCoach.Adaptation;
using Entrenate.Domain.Entidades;
using Xunit;

namespace Entrenate.Application.UnitTests.Coach.LiveCoach.Adaptation
{
    public sealed class LiveAdaptationOrchestratorTests
    {
        [Fact]
        public void Evaluate_TimeAndFatigue_UnionsChangesWithoutDuplicatesAndRespectsTime()
        {
            var session = CreateSession("Press", "Remo", "Curl", "Tríceps", "Gemelos");

            var result = LiveAdaptationOrchestrator.Evaluate(
                session,
                new LiveAdaptationSignals(20, LiveFatigueSeverity.Moderate));

            Assert.Equal(LiveCombinedAdaptationStatus.Proposal, result.Status);
            Assert.Equal(
                result.Changes.Count,
                result.Changes.Select(change => change.ExerciseSessionId).Distinct().Count());
            Assert.True(result.EstimatedMinutesAfter <= 20);
            Assert.DoesNotContain(
                result.Changes,
                change => change.ExerciseSessionId == session.Ejercicios.First().Id);
        }

        [Fact]
        public void Evaluate_PerformanceWithoutEvidence_DoesNotBlockTimeConstraint()
        {
            var session = CreateSession("Press", "Remo", "Curl", "Tríceps");

            var result = LiveAdaptationOrchestrator.Evaluate(
                session,
                new LiveAdaptationSignals(
                    12,
                    PerformanceSignalType: LivePerformanceSignalType.PerceivedWeakness));

            Assert.Equal(LiveCombinedAdaptationStatus.Proposal, result.Status);
            Assert.Contains(LiveAdaptationReason.TimeConstraint, result.AppliedReasons);
            Assert.DoesNotContain(LiveAdaptationReason.Performance, result.AppliedReasons);
            Assert.Null(result.PerformanceEvidence);
        }

        [Fact]
        public void Evaluate_PerformanceWithEvidence_ContributesItsReason()
        {
            var session = CreateSession("Press", "Remo", "Curl");
            session.Ejercicios.First().RegistrarOActualizarSerie(1, 100, 6, 0, DateTime.UtcNow);

            var result = LiveAdaptationOrchestrator.Evaluate(
                session,
                new LiveAdaptationSignals(
                    20,
                    PerformanceSignalType: LivePerformanceSignalType.MissedRepetitions));

            Assert.Equal(LiveCombinedAdaptationStatus.Proposal, result.Status);
            Assert.Contains(LiveAdaptationReason.Performance, result.AppliedReasons);
            Assert.True(result.PerformanceEvidence?.MissedPrescription);
        }

        [Fact]
        public void Evaluate_PreservesStartedExerciseAndIgnoresPreviouslyOmitted()
        {
            var session = CreateSession("Press", "Remo", "Curl", "Tríceps");
            session.Ejercicios.ElementAt(1).RegistrarOActualizarSerie(1, 80, 8, 1, DateTime.UtcNow);
            session.OmitirEjercicio(session.Ejercicios.Last().Id);

            var result = LiveAdaptationOrchestrator.Evaluate(
                session,
                new LiveAdaptationSignals(10, LiveFatigueSeverity.Moderate));

            Assert.DoesNotContain(result.Changes, change => change.ExerciseName == "Remo");
            Assert.DoesNotContain(result.Changes, change => change.ExerciseName == "Tríceps");
        }

        [Fact]
        public void Evaluate_WithSameState_IsDeterministicAndOrderedBySession()
        {
            var session = CreateSession("Press", "Remo", "Curl", "Tríceps", "Gemelos");
            var signals = new LiveAdaptationSignals(15, LiveFatigueSeverity.High);

            var first = LiveAdaptationOrchestrator.Evaluate(session, signals);
            var second = LiveAdaptationOrchestrator.Evaluate(session, signals);

            Assert.Equal(first.Changes, second.Changes);
            var orders = first.Changes
                .Select(change => session.Ejercicios.Single(exercise => exercise.Id == change.ExerciseSessionId).Orden)
                .ToArray();
            Assert.Equal(orders.OrderBy(order => order), orders);
        }

        [Fact]
        public void Evaluate_WhenOnlyCurrentRemains_DoesNotCreateEmptyProposal()
        {
            var session = CreateSession("Press");

            var result = LiveAdaptationOrchestrator.Evaluate(
                session,
                new LiveAdaptationSignals(20, LiveFatigueSeverity.High));

            Assert.Equal(LiveCombinedAdaptationStatus.NoApplicableChange, result.Status);
            Assert.Empty(result.Changes);
        }

        private static SesionEntrenamiento CreateSession(params string[] names)
        {
            var exercises = names.Select((name, index) =>
            {
                var exercise = new Ejercicio
                {
                    Id = Guid.NewGuid(),
                    Nombre = name,
                    GrupoMuscularPrincipal = "General",
                    Activo = true
                };
                var sessionExercise = new EjercicioSesion(
                    exercise.Id, index + 1, 3, 8, 10, 1, 2, 90);
                typeof(EjercicioSesion).GetProperty(
                    "Ejercicio",
                    BindingFlags.Public | BindingFlags.Instance)!.SetValue(sessionExercise, exercise);
                return sessionExercise;
            });

            return new SesionEntrenamiento(
                "user",
                Guid.NewGuid(),
                DateTime.UtcNow.AddMinutes(-20),
                exercises);
        }
    }
}
