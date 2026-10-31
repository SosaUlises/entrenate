using System.Linq;
using Entrenate.Application.TrainingSessions;
using Entrenate.Application.TrainingSessions.DTOs;
using Entrenate.Domain.Entidades;

namespace Entrenate.Application.Coach.LiveCoach.Rules
{
    public enum LiveCoachRestEligibilityStatus
    {
        /// <summary>No hay una serie recién completada inmediatamente anterior a la serie actual.</summary>
        NoRecentlyCompletedSet,

        /// <summary>Se completó la última serie de la sesión: no queda serie siguiente para la cual descansar.</summary>
        NoPendingSet,

        /// <summary>Corresponde descansar. <see cref="LiveCoachRestEligibility.RestSeconds"/> es la prescripción.</summary>
        Eligible
    }

    public sealed record LiveCoachRestEligibility(
        LiveCoachRestEligibilityStatus Status,
        int? RestSeconds = null);

    /// <summary>
    /// Decide de forma autoritativa, con el estado real de Training, si corresponde iniciar el descanso.
    /// Regla: existe una serie pendiente actual y la serie inmediatamente anterior (en el orden real de
    /// Training) está realmente completada según las reglas de negocio de Training. La duración es la
    /// prescripta para el ejercicio de esa serie completada, igual que el descanso guiado de la UI.
    /// </summary>
    public static class LiveCoachRestEligibilityResolver
    {
        /// <summary>
        /// Resuelve el descanso inmediatamente posterior a una confirmación ya persistida.
        /// La duración pertenece al ejercicio confirmado, no al siguiente target proyectado.
        /// </summary>
        public static LiveCoachRestEligibility ResolveAfterConfirmedSet(
            TrainingSessionDto session,
            Guid completedExerciseSessionId)
        {
            var hasPendingSet = session.Ejercicios
                .OrderBy(exercise => exercise.Orden)
                .Any(exercise => Enumerable.Range(1, exercise.SeriesObjetivo)
                    .Any(setNumber => !exercise.Series.Any(set =>
                        set.NumeroSerie == setNumber && set.Completada)));

            if (!hasPendingSet)
            {
                return new LiveCoachRestEligibility(LiveCoachRestEligibilityStatus.NoPendingSet);
            }

            var completedExercise = session.Ejercicios
                .FirstOrDefault(exercise => exercise.Id == completedExerciseSessionId);

            if (completedExercise is null)
            {
                return new LiveCoachRestEligibility(
                    LiveCoachRestEligibilityStatus.NoRecentlyCompletedSet);
            }

            return new LiveCoachRestEligibility(
                LiveCoachRestEligibilityStatus.Eligible,
                completedExercise.DescansoObjetivoSegundos);
        }

        public static LiveCoachRestEligibility Resolve(SesionEntrenamiento session)
        {
            var current = CurrentPendingTrainingSetResolver.Resolve(session);
            if (current is null)
            {
                return new LiveCoachRestEligibility(LiveCoachRestEligibilityStatus.NoPendingSet);
            }

            var (previousExercise, previousSetNumber) = ResolvePreviousSetPosition(session, current);
            if (previousExercise is null || previousSetNumber < 1)
            {
                return new LiveCoachRestEligibility(LiveCoachRestEligibilityStatus.NoRecentlyCompletedSet);
            }

            var previousSet = previousExercise.Series
                .FirstOrDefault(serie => serie.NumeroSerie == previousSetNumber);

            var previousSetCompleted = previousSet != null && previousSet.EstaRealmenteCompletada();

            return previousSetCompleted
                ? new LiveCoachRestEligibility(
                    LiveCoachRestEligibilityStatus.Eligible,
                    previousExercise.DescansoObjetivoSegundos)
                : new LiveCoachRestEligibility(LiveCoachRestEligibilityStatus.NoRecentlyCompletedSet);
        }

        private static (EjercicioSesion? Exercise, int SetNumber) ResolvePreviousSetPosition(
            SesionEntrenamiento session,
            CurrentPendingTrainingSet current)
        {
            if (current.SetNumber > 1)
            {
                return (current.ExerciseSession, current.SetNumber - 1);
            }

            // Primera serie del ejercicio actual: la serie anterior es la última serie objetivo del ejercicio previo.
            var previousExercise = session.Ejercicios
                .Where(item => item.Orden < current.ExerciseSession.Orden)
                .OrderByDescending(item => item.Orden)
                .FirstOrDefault();

            return previousExercise is null
                ? (null, 0)
                : (previousExercise, previousExercise.SeriesObjetivo);
        }
    }
}
