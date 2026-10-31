using System.Collections.Generic;
using System.Linq;
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
            foreach (var exercise in session.Ejercicios.OrderBy(item => item.Orden))
            {
                var completedSetNumbers = new HashSet<int>(
                    exercise.Series
                        .Where(s => s.EstaRealmenteCompletada())
                        .Select(s => s.NumeroSerie));

                for (var setNumber = 1; setNumber <= exercise.SeriesObjetivo; setNumber++)
                {
                    if (!completedSetNumbers.Contains(setNumber))
                    {
                        return new CurrentPendingTrainingSet(exercise, setNumber);
                    }
                }
            }

            return null;
        }
    }
}
