using System;
using System.Linq;
using System.Reflection;
using Entrenate.Application.Coach.LiveCoach.Context;
using Entrenate.Domain.Entidades;
using Xunit;

namespace Entrenate.Application.UnitTests.Coach.LiveCoach.Context
{
    public sealed class LiveCoachContextBuilderTests
    {
        private readonly LiveCoachContextBuilder _builder = new();

        [Fact]
        public void Build_WithActiveRestSnapshot_ReportsTimerAsActive()
        {
            var nowUtc = DateTime.UtcNow;

            var context = BuildRestContext(true, 120, nowUtc, nowUtc);

            Assert.True(context.Rest.IsActive);
        }

        [Fact]
        public void Build_WithFreshRestSnapshot_PreservesRemainingSeconds()
        {
            var nowUtc = DateTime.UtcNow;

            var context = BuildRestContext(true, 120, nowUtc, nowUtc);

            Assert.Equal(120, context.Rest.RemainingSecondsAtCapture);
        }

        [Fact]
        public void Build_WithElapsedTime_DiscountsItFromRemainingSeconds()
        {
            var nowUtc = DateTime.UtcNow;

            var context = BuildRestContext(true, 120, nowUtc.AddSeconds(-15), nowUtc);

            Assert.True(context.Rest.IsActive);
            Assert.Equal(105, context.Rest.RemainingSecondsAtCapture);
        }

        [Fact]
        public void Build_WhenRestElapsedDuringProcessing_ReportsTimerAsExpired()
        {
            var nowUtc = DateTime.UtcNow;

            var context = BuildRestContext(true, 10, nowUtc.AddSeconds(-12), nowUtc);

            Assert.False(context.Rest.IsActive);
            Assert.Equal(0, context.Rest.RemainingSecondsAtCapture);
        }

        [Fact]
        public void Build_WithInactiveRestSnapshot_ReportsNoActiveTimer()
        {
            var nowUtc = DateTime.UtcNow;

            var context = BuildRestContext(false, 120, nowUtc, nowUtc);

            Assert.False(context.Rest.IsActive);
            Assert.Null(context.Rest.TargetSeconds);
            Assert.Null(context.Rest.RemainingSecondsAtCapture);
        }

        private Entrenate.Application.Coach.LiveCoach.Models.LiveCoachSessionContext BuildRestContext(
            bool isActive,
            int remainingSeconds,
            DateTime capturedAtUtc,
            DateTime nowUtc)
        {
            var session = CreateSession();
            var exercise = session.Ejercicios.First();
            var snapshot = new ClientSnapshotInput(
                exercise.EjercicioId,
                1,
                isActive,
                remainingSeconds,
                capturedAtUtc);

            return _builder.Build(session, snapshot, nowUtc);
        }

        private static SesionEntrenamiento CreateSession()
        {
            var exercise = new Ejercicio
            {
                Id = Guid.NewGuid(),
                Nombre = "Press de banca",
                GrupoMuscularPrincipal = "Pecho",
                Activo = true
            };
            var sessionExercise = new EjercicioSesion(
                exercise.Id,
                orden: 1,
                seriesObjetivo: 4,
                repeticionesMinimasObjetivo: 8,
                repeticionesMaximasObjetivo: 10,
                rirObjetivoMinimo: 1,
                rirObjetivoMaximo: 2,
                descansoObjetivoSegundos: 120,
                notas: null);

            typeof(EjercicioSesion)
                .GetProperty("Ejercicio", BindingFlags.Public | BindingFlags.Instance)?
                .SetValue(sessionExercise, exercise);

            return new SesionEntrenamiento(
                "live-coach-user",
                Guid.NewGuid(),
                DateTime.UtcNow.AddMinutes(-10),
                [sessionExercise]);
        }
    }
}
