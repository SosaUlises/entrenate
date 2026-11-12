using System.Reflection;
using Entrenate.Application.Coach.LiveCoach.Adaptation;
using Entrenate.Domain.Entidades;
using Xunit;

namespace Entrenate.Application.UnitTests.Coach.LiveCoach.Adaptation
{
    public sealed class LiveTimeConstraintAdaptationEngineTests
    {
        [Fact]
        public void Evaluate_WhenAvailableTimeFits_ReturnsNoProposal()
        {
            var session = CreateSession(("Press", 3), ("Remo", 3));

            var result = LiveTimeConstraintAdaptationEngine.Evaluate(session, 20);

            Assert.Equal(LiveTimeConstraintResultStatus.FitsAvailableTime, result.Status);
            Assert.Empty(result.Changes);
            Assert.Equal(result.EstimatedMinutesBefore, result.EstimatedMinutesAfter);
        }

        [Fact]
        public void Evaluate_WhenTimeIsShort_OmitsLastExercise()
        {
            var session = CreateSession(("Press", 3), ("Remo", 3));

            var result = LiveTimeConstraintAdaptationEngine.Evaluate(session, 8);

            Assert.Equal(LiveTimeConstraintResultStatus.Proposal, result.Status);
            var change = Assert.Single(result.Changes);
            Assert.Equal("Remo", change.ExerciseName);
            Assert.True(result.EstimatedMinutesAfter < result.EstimatedMinutesBefore);
        }

        [Fact]
        public void Evaluate_WhenNeeded_OmitsSeveralExercisesFromTheEnd()
        {
            var session = CreateSession(("Press", 3), ("Remo", 3), ("Curl", 3));

            var result = LiveTimeConstraintAdaptationEngine.Evaluate(session, 7);

            Assert.Equal(["Remo", "Curl"], result.Changes.Select(change => change.ExerciseName));
        }

        [Fact]
        public void Evaluate_AlwaysPreservesAuthoritativeCurrentExercise()
        {
            var session = CreateSession(("Press", 4), ("Remo", 3));

            var result = LiveTimeConstraintAdaptationEngine.Evaluate(session, 5);

            Assert.DoesNotContain(result.Changes, change => change.ExerciseName == "Press");
            Assert.Equal("Remo", Assert.Single(result.Changes).ExerciseName);
        }

        [Fact]
        public void Evaluate_PrioritizesStartedExerciseOverUnstartedExercise()
        {
            var session = CreateSession(("Press", 3), ("Remo", 3), ("Curl", 3));
            CompleteSets(session.Ejercicios.ElementAt(1), 1);

            var result = LiveTimeConstraintAdaptationEngine.Evaluate(session, 10);

            Assert.Contains(result.Changes, change => change.ExerciseName == "Curl");
            Assert.DoesNotContain(result.Changes, change => change.ExerciseName == "Remo");
        }

        [Fact]
        public void Evaluate_IgnoresPreviouslyOmittedExercises()
        {
            var session = CreateSession(("Press", 3), ("Remo", 3), ("Curl", 3));
            session.OmitirEjercicio(session.Ejercicios.Last().Id);

            var result = LiveTimeConstraintAdaptationEngine.Evaluate(session, 8);

            Assert.Equal("Remo", Assert.Single(result.Changes).ExerciseName);
            Assert.DoesNotContain(result.Changes, change => change.ExerciseName == "Curl");
        }

        [Fact]
        public void Evaluate_DoesNotCountInvalidPartialSetAsCompleted()
        {
            var session = CreateSession(("Press", 3));
            AddIncompleteSet(session.Ejercicios.Single(), 1);

            var result = LiveTimeConstraintAdaptationEngine.Evaluate(session, 5);

            Assert.Equal(LiveTimeConstraintResultStatus.CannotAdapt, result.Status);
            Assert.Equal(6, result.EstimatedMinutesBefore);
        }

        [Fact]
        public void Evaluate_WhenSessionIsComplete_ReturnsNaturalEmptyState()
        {
            var session = CreateSession(("Press", 2));
            CompleteSets(session.Ejercicios.Single(), 2);

            var result = LiveTimeConstraintAdaptationEngine.Evaluate(session, 20);

            Assert.Equal(LiveTimeConstraintResultStatus.NoPendingWork, result.Status);
            Assert.Empty(result.Changes);
        }

        [Fact]
        public void Evaluate_WithFiveMinutes_KeepsCurrentAndReportsBestConservativeCut()
        {
            var session = CreateSession(("Press", 3), ("Remo", 3));

            var result = LiveTimeConstraintAdaptationEngine.Evaluate(session, 5);

            Assert.Equal(LiveTimeConstraintResultStatus.Proposal, result.Status);
            Assert.Equal("Remo", Assert.Single(result.Changes).ExerciseName);
            Assert.True(result.EstimatedMinutesAfter > result.RequestedRemainingMinutes);
        }

        private static SesionEntrenamiento CreateSession(params (string Name, int Sets)[] values)
        {
            var exercises = values.Select((value, index) =>
            {
                var exercise = new Ejercicio
                {
                    Id = Guid.NewGuid(),
                    Nombre = value.Name,
                    GrupoMuscularPrincipal = "General",
                    Activo = true
                };
                var sessionExercise = new EjercicioSesion(
                    exercise.Id,
                    index + 1,
                    value.Sets,
                    8,
                    10,
                    1,
                    2,
                    90);
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

        private static void AddIncompleteSet(EjercicioSesion exercise, int number)
        {
            var set = new SerieEntrenamiento();
            set.ConfigurarNumeroSerie(number);
            set.RegistrarRendimiento(100, 8, 1, DateTime.UtcNow);
            var field = typeof(EjercicioSesion).GetField(
                "_series",
                BindingFlags.NonPublic | BindingFlags.Instance)!;
            ((List<SerieEntrenamiento>)field.GetValue(exercise)!).Add(set);
        }
    }
}
