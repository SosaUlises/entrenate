using System.Reflection;
using Entrenate.Application.Coach.LiveCoach.Adaptation;
using Entrenate.Domain.Entidades;
using Xunit;

namespace Entrenate.Application.UnitTests.Coach.LiveCoach.Adaptation
{
    public sealed class LiveFatigueAdaptationEngineTests
    {
        [Fact]
        public void Evaluate_Moderate_PreservesCurrentAndStartedWorkAndCutsFromEnd()
        {
            var session = CreateSession("Press", "Remo", "Curl", "Tríceps", "Gemelos");
            CompleteSets(session.Ejercicios.ElementAt(1), 1);

            var result = LiveFatigueAdaptationEngine.Evaluate(
                session,
                LiveFatigueSeverity.Moderate);

            Assert.Equal(LiveFatigueAdaptationResultStatus.Proposal, result.Status);
            Assert.Equal("Gemelos", Assert.Single(result.Changes).ExerciseName);
            Assert.DoesNotContain(result.Changes, change => change.ExerciseName is "Press" or "Remo");
        }

        [Fact]
        public void Evaluate_High_RemovesUnstartedAndOnlyLaterStartedWork()
        {
            var session = CreateSession("Press", "Remo", "Curl", "Tríceps");
            CompleteSets(session.Ejercicios.ElementAt(1), 1);
            CompleteSets(session.Ejercicios.ElementAt(2), 1);

            var result = LiveFatigueAdaptationEngine.Evaluate(
                session,
                LiveFatigueSeverity.High);

            Assert.Equal(["Curl", "Tríceps"], result.Changes.Select(change => change.ExerciseName));
            Assert.DoesNotContain(result.Changes, change => change.ExerciseName is "Press" or "Remo");
        }

        [Fact]
        public void Evaluate_IgnoresAlreadyOmittedExercise()
        {
            var session = CreateSession("Press", "Remo", "Curl");
            session.OmitirEjercicio(session.Ejercicios.Last().Id);

            var result = LiveFatigueAdaptationEngine.Evaluate(session, LiveFatigueSeverity.High);

            Assert.Equal("Remo", Assert.Single(result.Changes).ExerciseName);
        }

        [Fact]
        public void Evaluate_WhenOnlyCurrentRemains_ReturnsNothingToReduce()
        {
            var session = CreateSession("Press");

            var result = LiveFatigueAdaptationEngine.Evaluate(session, LiveFatigueSeverity.High);

            Assert.Equal(LiveFatigueAdaptationResultStatus.NothingToReduce, result.Status);
            Assert.Empty(result.Changes);
        }

        [Fact]
        public void Evaluate_WhenSessionIsComplete_ReturnsNoPendingWork()
        {
            var session = CreateSession("Press");
            CompleteSets(session.Ejercicios.Single(), 3);

            var result = LiveFatigueAdaptationEngine.Evaluate(session, LiveFatigueSeverity.Moderate);

            Assert.Equal(LiveFatigueAdaptationResultStatus.NoPendingWork, result.Status);
        }

        [Fact]
        public void Evaluate_WithSameState_IsDeterministic()
        {
            var session = CreateSession("Press", "Remo", "Curl", "Tríceps");

            var first = LiveFatigueAdaptationEngine.Evaluate(session, LiveFatigueSeverity.Moderate);
            var second = LiveFatigueAdaptationEngine.Evaluate(session, LiveFatigueSeverity.Moderate);

            Assert.Equal(first.Changes, second.Changes);
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

        private static void CompleteSets(EjercicioSesion exercise, int count)
        {
            for (var number = 1; number <= count; number++)
                exercise.RegistrarOActualizarSerie(number, 100, 8, 1, DateTime.UtcNow);
        }
    }
}
