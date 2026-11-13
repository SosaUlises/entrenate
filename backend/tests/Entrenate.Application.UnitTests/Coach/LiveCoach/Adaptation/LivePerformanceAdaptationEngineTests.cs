using System.Reflection;
using Entrenate.Application.Coach.LiveCoach.Adaptation;
using Entrenate.Domain.Entidades;
using Xunit;

namespace Entrenate.Application.UnitTests.Coach.LiveCoach.Adaptation
{
    public sealed class LivePerformanceAdaptationEngineTests
    {
        [Fact]
        public void Evaluate_TargetEightToTenAndActualSix_DetectsMissedPrescription()
        {
            var session = CreateSession(8, 10, 2, 3, includeFutureExercise: true);
            CompleteSet(session.Ejercicios.First(), 1, repetitions: 6, rir: 0);

            var result = Evaluate(session);

            Assert.Equal(LivePerformanceAdaptationResultStatus.Proposal, result.Status);
            Assert.True(result.Evidence?.MissedPrescription);
            Assert.Equal("Remo", Assert.Single(result.Changes).ExerciseName);
        }

        [Fact]
        public void Evaluate_TargetEightToTenAndActualEight_IsWithinPrescription()
        {
            var session = CreateSession(8, 10, 2, 3, includeFutureExercise: true);
            CompleteSet(session.Ejercicios.First(), 1, repetitions: 8, rir: 2);

            var result = Evaluate(session);

            Assert.Equal(LivePerformanceAdaptationResultStatus.WithinPrescription, result.Status);
            Assert.False(result.Evidence?.MissedPrescription);
            Assert.Empty(result.Changes);
        }

        [Theory]
        [InlineData(8, true)]
        [InlineData(10, false)]
        public void Evaluate_ExactTarget_UsesMinimumAsDeterministicThreshold(
            int actualRepetitions,
            bool expectedMissed)
        {
            var session = CreateSession(10, 10, 2, 2, includeFutureExercise: true);
            CompleteSet(session.Ejercicios.First(), 1, actualRepetitions, rir: 2);

            var result = Evaluate(session);

            Assert.Equal(expectedMissed, result.Evidence?.MissedPrescription);
        }

        [Fact]
        public void Evaluate_RirBelowTarget_DetectsHigherEffort()
        {
            var session = CreateSession(8, 10, 2, 2, includeFutureExercise: true);
            CompleteSet(session.Ejercicios.First(), 1, repetitions: 8, rir: 0);

            var result = Evaluate(session);

            Assert.Equal(LivePerformanceAdaptationResultStatus.Proposal, result.Status);
            Assert.True(result.Evidence?.ExceededTargetEffort);
        }

        [Fact]
        public void Evaluate_WithoutPreviousCompletedSet_ReturnsInsufficientContext()
        {
            var session = CreateSession(8, 10, 2, 3, includeFutureExercise: true);

            Assert.Equal(
                LivePerformanceAdaptationResultStatus.InsufficientContext,
                Evaluate(session).Status);
        }

        [Fact]
        public void Evaluate_InvalidPartialSet_DoesNotCountAsEvidence()
        {
            var session = CreateSession(8, 10, 2, 3, includeFutureExercise: true);
            AddIncompleteSet(session.Ejercicios.First(), 1, 6, 0);

            Assert.Equal(
                LivePerformanceAdaptationResultStatus.InsufficientContext,
                Evaluate(session).Status);
        }

        [Fact]
        public void Evaluate_PreservesCurrentStartedAndCompletedPerformance()
        {
            var session = CreateSession(8, 10, 2, 3, includeFutureExercise: true);
            CompleteSet(session.Ejercicios.First(), 1, repetitions: 6, rir: 0);

            var result = Evaluate(session);

            Assert.DoesNotContain(result.Changes, change => change.ExerciseName == "Press");
            var completed = Assert.Single(session.Ejercicios.First().Series);
            Assert.Equal(6, completed.Repeticiones);
            Assert.Equal(0, completed.Rir);
        }

        [Fact]
        public void Evaluate_WhenNoUnstartedFutureExerciseExists_DoesNotCreateProposal()
        {
            var session = CreateSession(8, 10, 2, 3, includeFutureExercise: false);
            CompleteSet(session.Ejercicios.First(), 1, repetitions: 6, rir: 0);

            var result = Evaluate(session);

            Assert.Equal(
                LivePerformanceAdaptationResultStatus.UnderperformanceWithoutSafeCut,
                result.Status);
            Assert.Empty(result.Changes);
        }

        private static LivePerformanceAdaptationResult Evaluate(SesionEntrenamiento session)
            => LivePerformanceAdaptationEngine.Evaluate(
                session,
                LivePerformanceSignalType.PerceivedWeakness);

        private static SesionEntrenamiento CreateSession(
            int minimumRepetitions,
            int maximumRepetitions,
            int minimumRir,
            int maximumRir,
            bool includeFutureExercise)
        {
            var names = includeFutureExercise ? new[] { "Press", "Remo" } : new[] { "Press" };
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
                    exercise.Id,
                    index + 1,
                    3,
                    minimumRepetitions,
                    maximumRepetitions,
                    minimumRir,
                    maximumRir,
                    90);
                typeof(EjercicioSesion).GetProperty(
                    "Ejercicio",
                    BindingFlags.Public | BindingFlags.Instance)!.SetValue(sessionExercise, exercise);
                return sessionExercise;
            });

            return new SesionEntrenamiento(
                "user",
                Guid.NewGuid(),
                DateTime.UtcNow.AddMinutes(-10),
                exercises);
        }

        private static void CompleteSet(
            EjercicioSesion exercise,
            int number,
            int repetitions,
            int? rir)
            => exercise.RegistrarOActualizarSerie(number, 100, repetitions, rir, DateTime.UtcNow);

        private static void AddIncompleteSet(
            EjercicioSesion exercise,
            int number,
            int repetitions,
            int? rir)
        {
            var set = new SerieEntrenamiento();
            set.ConfigurarNumeroSerie(number);
            set.RegistrarRendimiento(100, repetitions, rir, DateTime.UtcNow);
            var field = typeof(EjercicioSesion).GetField(
                "_series",
                BindingFlags.NonPublic | BindingFlags.Instance)!;
            ((List<SerieEntrenamiento>)field.GetValue(exercise)!).Add(set);
        }
    }
}
