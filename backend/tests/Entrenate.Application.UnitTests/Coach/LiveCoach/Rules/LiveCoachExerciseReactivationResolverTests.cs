using System.Reflection;
using Entrenate.Application.Coach.LiveCoach.Rules;
using Entrenate.Domain.Entidades;
using Xunit;

namespace Entrenate.Application.UnitTests.Coach.LiveCoach.Rules
{
    public sealed class LiveCoachExerciseReactivationResolverTests
    {
        [Fact]
        public void Resolve_NormalizesCaseAndDiacritics_AndFindsOmittedExercise()
        {
            var session = CreateSession("Hip Thrust");
            var exercise = session.Ejercicios.Single();
            session.OmitirEjercicio(exercise.Id);

            var result = LiveCoachExerciseReactivationResolver.Resolve(
                session,
                "REACTIVÁ hip thrust");

            Assert.Equal(LiveCoachExerciseReactivationStatus.Found, result.Status);
            Assert.Same(exercise, result.Exercise);
        }

        [Fact]
        public void Resolve_PartialNameMatchesMultipleOmittedExercises_ReturnsAmbiguous()
        {
            var session = CreateSession("Press banca barra", "Press banca mancuernas");
            foreach (var exercise in session.Ejercicios)
                session.OmitirEjercicio(exercise.Id);

            var result = LiveCoachExerciseReactivationResolver.Resolve(
                session,
                "reactivá press banca");

            Assert.Equal(LiveCoachExerciseReactivationStatus.Ambiguous, result.Status);
            Assert.Null(result.Exercise);
        }

        [Fact]
        public void Resolve_ActiveExercise_ReturnsAlreadyActive()
        {
            var session = CreateSession("Hip Thrust");

            var result = LiveCoachExerciseReactivationResolver.Resolve(
                session,
                "volvamos a hip thrust");

            Assert.Equal(LiveCoachExerciseReactivationStatus.AlreadyActive, result.Status);
        }

        [Fact]
        public void Resolve_UnknownNameWithOmittedExercise_ReturnsNotFound()
        {
            var session = CreateSession("Hip Thrust");
            session.OmitirEjercicio(session.Ejercicios.Single().Id);

            var result = LiveCoachExerciseReactivationResolver.Resolve(
                session,
                "reactivá sentadilla");

            Assert.Equal(LiveCoachExerciseReactivationStatus.NotFound, result.Status);
        }

        private static SesionEntrenamiento CreateSession(params string[] names)
        {
            var exercises = names.Select((name, index) =>
            {
                var catalogExercise = new Ejercicio
                {
                    Id = Guid.NewGuid(),
                    Nombre = name,
                    GrupoMuscularPrincipal = "General",
                    Activo = true
                };
                var sessionExercise = new EjercicioSesion(
                    catalogExercise.Id, index + 1, 3, 8, 10, 1, 2, 120);
                typeof(EjercicioSesion)
                    .GetProperty("Ejercicio", BindingFlags.Public | BindingFlags.Instance)!
                    .SetValue(sessionExercise, catalogExercise);
                return sessionExercise;
            });

            return new SesionEntrenamiento(
                "user-1",
                Guid.NewGuid(),
                DateTime.UtcNow,
                exercises);
        }
    }
}
