using Entrenate.Domain.Entidades;

namespace Entrenate.Application.TrainingSessions
{
    public sealed record CurrentPendingTrainingSet(
        EjercicioSesion ExerciseSession,
        int SetNumber);

    public static class CurrentPendingTrainingSetResolver
    {
        public static CurrentPendingTrainingSet? Resolve(SesionEntrenamiento session)
        {
            var exercise = session.Ejercicios
                .OrderBy(item => item.Orden)
                .FirstOrDefault(item => item.Series.Count < item.SeriesObjetivo);

            return exercise is null
                ? null
                : new CurrentPendingTrainingSet(exercise, exercise.Series.Count + 1);
        }
    }
}
