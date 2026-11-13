using Entrenate.Application.TrainingSessions;
using Entrenate.Domain.Entidades;

namespace Entrenate.Application.Coach.LiveCoach.Adaptation
{
    public static class LiveTimeConstraintAdaptationEngine
    {
        public const int MinimumRemainingMinutes = 5;
        public const int MaximumRemainingMinutes = 120;

        // Shared with the deterministic duration policies used by Adaptive Training.
        private const int SetExecutionSeconds = 45;
        private const int TransitionSeconds = 60;

        public static LiveTimeConstraintResult Evaluate(
            SesionEntrenamiento session,
            int remainingMinutes)
        {
            ArgumentNullException.ThrowIfNull(session);
            if (remainingMinutes is < MinimumRemainingMinutes or > MaximumRemainingMinutes)
                throw new ArgumentOutOfRangeException(nameof(remainingMinutes));

            var current = CurrentPendingTrainingSetResolver.Resolve(session);
            if (current is null)
            {
                return new(
                    LiveTimeConstraintResultStatus.NoPendingWork,
                    remainingMinutes,
                    0,
                    0,
                    []);
            }

            var pending = session.Ejercicios
                .Where(exercise => !exercise.Omitido)
                .OrderBy(exercise => exercise.Orden)
                .Select(ToPendingExercise)
                .Where(exercise => exercise.PendingSets > 0)
                .ToList();
            var beforeSeconds = EstimateSeconds(pending);
            var beforeMinutes = ToMinutes(beforeSeconds);
            if (beforeSeconds <= remainingMinutes * 60)
            {
                return new(
                    LiveTimeConstraintResultStatus.FitsAvailableTime,
                    remainingMinutes,
                    beforeMinutes,
                    beforeMinutes,
                    []);
            }

            var retained = pending.ToList();
            var removed = new List<PendingExercise>();
            RemoveFromEnd(
                retained,
                removed,
                current.ExerciseSession.Id,
                remainingMinutes,
                started: false);
            RemoveFromEnd(
                retained,
                removed,
                current.ExerciseSession.Id,
                remainingMinutes,
                started: true);

            var afterMinutes = ToMinutes(EstimateSeconds(retained));
            if (removed.Count == 0)
            {
                return new(
                    LiveTimeConstraintResultStatus.CannotAdapt,
                    remainingMinutes,
                    beforeMinutes,
                    afterMinutes,
                    []);
            }

            var changes = removed
                .OrderBy(exercise => exercise.Order)
                .Select(exercise => new LiveAdaptationChange(
                    LiveAdaptationChangeType.OmitExercise,
                    exercise.ExerciseSessionId,
                    exercise.Name))
                .ToArray();
            return new(
                LiveTimeConstraintResultStatus.Proposal,
                remainingMinutes,
                beforeMinutes,
                afterMinutes,
                changes);
        }

        private static void RemoveFromEnd(
            List<PendingExercise> retained,
            List<PendingExercise> removed,
            Guid currentExerciseSessionId,
            int remainingMinutes,
            bool started)
        {
            while (EstimateSeconds(retained) > remainingMinutes * 60)
            {
                var candidate = retained
                    .Where(exercise =>
                        exercise.ExerciseSessionId != currentExerciseSessionId &&
                        (exercise.CompletedSets > 0) == started)
                    .OrderByDescending(exercise => exercise.Order)
                    .FirstOrDefault();
                if (candidate is null)
                    return;

                retained.Remove(candidate);
                removed.Add(candidate);
            }
        }

        private static PendingExercise ToPendingExercise(EjercicioSesion exercise)
        {
            var completedSets = exercise.Series
                .Where(set => set.EstaRealmenteCompletada())
                .Select(set => set.NumeroSerie)
                .Distinct()
                .Count(number => number >= 1 && number <= exercise.SeriesObjetivo);

            return new PendingExercise(
                exercise.Id,
                exercise.Ejercicio.Nombre,
                exercise.Orden,
                completedSets,
                Math.Max(0, exercise.SeriesObjetivo - completedSets),
                exercise.DescansoObjetivoSegundos);
        }

        private static int EstimateSeconds(IReadOnlyCollection<PendingExercise> exercises)
        {
            if (exercises.Count == 0)
                return 0;

            return checked(
                Math.Max(0, exercises.Count - 1) * TransitionSeconds +
                exercises.Sum(exercise =>
                    exercise.PendingSets * SetExecutionSeconds +
                    Math.Max(0, exercise.PendingSets - 1) * exercise.RestSeconds));
        }

        private static int ToMinutes(int seconds)
            => (int)Math.Ceiling(seconds / 60m);

        private sealed record PendingExercise(
            Guid ExerciseSessionId,
            string Name,
            int Order,
            int CompletedSets,
            int PendingSets,
            int RestSeconds);
    }
}
