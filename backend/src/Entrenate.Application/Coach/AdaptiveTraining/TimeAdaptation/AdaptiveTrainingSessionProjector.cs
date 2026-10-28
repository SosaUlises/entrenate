using Entrenate.Domain.Entidades;

namespace Entrenate.Application.Coach.AdaptiveTraining.TimeAdaptation
{
    public static class AdaptiveTrainingSessionProjector
    {
        public static PlannedAdaptiveTrainingSession Project(
            DiaRutina routineDay)
        {
            ArgumentNullException.ThrowIfNull(routineDay);

            var exercises = routineDay.Ejercicios
                .OrderBy(exercise => exercise.Orden)
                .ThenBy(
                    exercise => exercise.Ejercicio.Codigo,
                    StringComparer.Ordinal)
                .ThenBy(exercise => exercise.EjercicioId)
                .Select(exercise => new PlannedAdaptiveTrainingExercise(
                    exercise.Id,
                    exercise.EjercicioId,
                    exercise.Ejercicio.Codigo,
                    exercise.Ejercicio.Nombre,
                    exercise.Orden,
                    exercise.Ejercicio.PatronMovimiento,
                    exercise.Ejercicio.TipoEjercicio,
                    exercise.Ejercicio.GrupoMuscularPrincipal,
                    exercise.IsRequiredForAdaptation,
                    new AdaptiveTrainingPrescription(
                        exercise.CantidadSeries,
                        exercise.RepeticionesMinimas,
                        exercise.RepeticionesMaximas,
                        exercise.RirObjetivoMinimo,
                        exercise.RirObjetivoMaximo,
                        exercise.DescansoSegundos,
                        exercise.Notas)))
                .ToArray();

            return new PlannedAdaptiveTrainingSession(
                routineDay.Id,
                exercises);
        }
    }
}
