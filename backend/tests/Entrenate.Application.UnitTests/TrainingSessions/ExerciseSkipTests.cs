using Entrenate.Application.TrainingSessions;
using Entrenate.Domain.Entidades;
using Xunit;

namespace Entrenate.Application.UnitTests.TrainingSessions
{
    public sealed class ExerciseSkipTests
    {
        [Fact]
        public void OmitirEjercicio_PreservesCompletedSets_AndResolverAdvances()
        {
            var first = CreateExercise(1);
            var second = CreateExercise(2);
            var session = CreateSession(first, second);
            first.RegistrarOActualizarSerie(1, 80, 8, 2, DateTime.UtcNow);

            session.OmitirEjercicio(first.Id);

            var current = CurrentPendingTrainingSetResolver.Resolve(session);
            Assert.True(first.Omitido);
            Assert.Single(first.Series);
            Assert.Equal(second.Id, current?.ExerciseSession.Id);
            Assert.Equal(1, current?.SetNumber);
        }

        [Fact]
        public void ReactivarEjercicio_RestoresFirstRealGap()
        {
            var first = CreateExercise(1);
            var second = CreateExercise(2);
            var session = CreateSession(first, second);
            first.RegistrarOActualizarSerie(1, 80, 8, 2, DateTime.UtcNow);
            session.OmitirEjercicio(first.Id);

            session.ReactivarEjercicio(first.Id);

            var current = CurrentPendingTrainingSetResolver.Resolve(session);
            Assert.False(first.Omitido);
            Assert.Equal(first.Id, current?.ExerciseSession.Id);
            Assert.Equal(2, current?.SetNumber);
        }

        [Fact]
        public void Resolve_WhenCurrentCompletesAndAllFollowingExercisesAreOmitted_ReturnsNoTarget()
        {
            var current = CreateExercise(1);
            var omittedSecond = CreateExercise(2);
            var omittedThird = CreateExercise(3);
            var session = CreateSession(current, omittedSecond, omittedThird);
            session.OmitirEjercicio(omittedSecond.Id);
            session.OmitirEjercicio(omittedThird.Id);

            CompleteTargetSets(current);

            Assert.Null(CurrentPendingTrainingSetResolver.Resolve(session));
            Assert.True(omittedSecond.Omitido);
            Assert.True(omittedThird.Omitido);
        }

        [Fact]
        public void Resolve_WhenCompletedExerciseIsFollowedOnlyByOmittedExercises_ReturnsNoTarget()
        {
            var completed = CreateExercise(1);
            var omittedSecond = CreateExercise(2);
            var omittedThird = CreateExercise(3);
            var session = CreateSession(completed, omittedSecond, omittedThird);
            CompleteTargetSets(completed);
            session.OmitirEjercicio(omittedSecond.Id);
            session.OmitirEjercicio(omittedThird.Id);

            Assert.Null(CurrentPendingTrainingSetResolver.Resolve(session));
        }

        [Fact]
        public void Resolve_WhenOmittedExerciseWithCompletedSetsExists_StillIgnoresIt()
        {
            var completed = CreateExercise(1);
            var omittedWithWork = CreateExercise(2);
            var session = CreateSession(completed, omittedWithWork);
            CompleteTargetSets(completed);
            omittedWithWork.RegistrarOActualizarSerie(1, 60, 10, 2, DateTime.UtcNow);
            session.OmitirEjercicio(omittedWithWork.Id);

            Assert.Null(CurrentPendingTrainingSetResolver.Resolve(session));
            Assert.Single(omittedWithWork.Series);
        }

        [Fact]
        public void Resolve_WhenFollowingExerciseIsReactivated_FindsItsFirstRealGap()
        {
            var completed = CreateExercise(1);
            var reactivated = CreateExercise(2);
            var session = CreateSession(completed, reactivated);
            CompleteTargetSets(completed);
            session.OmitirEjercicio(reactivated.Id);
            session.ReactivarEjercicio(reactivated.Id);

            var current = CurrentPendingTrainingSetResolver.Resolve(session);

            Assert.Equal(reactivated.Id, current?.ExerciseSession.Id);
            Assert.Equal(1, current?.SetNumber);
        }

        [Fact]
        public void OmitirOrReactivar_AfterSessionCompletes_Fails()
        {
            var exercise = CreateExercise(1);
            var session = CreateSession(exercise);
            session.OmitirEjercicio(exercise.Id);
            session.Completar(DateTime.UtcNow.AddMinutes(10));

            Assert.Throws<InvalidOperationException>(() => session.ReactivarEjercicio(exercise.Id));
            Assert.True(exercise.Omitido);
        }

        [Fact]
        public void Completar_WithOmittedExercise_RemainsAllowedWithoutFakeSets()
        {
            var exercise = CreateExercise(1);
            var session = CreateSession(exercise);
            session.OmitirEjercicio(exercise.Id);

            session.Completar(DateTime.UtcNow.AddMinutes(10));

            Assert.True(exercise.Omitido);
            Assert.Empty(exercise.Series);
        }

        private static SesionEntrenamiento CreateSession(params EjercicioSesion[] exercises)
            => new("user-1", Guid.NewGuid(), DateTime.UtcNow, exercises);

        private static EjercicioSesion CreateExercise(int order)
            => new(Guid.NewGuid(), order, 3, 8, 10, 1, 2, 120);

        private static void CompleteTargetSets(EjercicioSesion exercise)
        {
            for (var setNumber = 1; setNumber <= exercise.SeriesObjetivo; setNumber++)
            {
                exercise.RegistrarOActualizarSerie(
                    setNumber,
                    80,
                    8,
                    2,
                    DateTime.UtcNow);
            }
        }
    }
}
