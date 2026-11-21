using Entrenate.Application.TrainingSessions;
using Entrenate.Domain.Entidades;

namespace Entrenate.Application.Coach.LiveCoach.Proactive
{
    public sealed record LiveCoachPerformanceTrendEvidence(
        EjercicioSesion Exercise,
        IReadOnlyCollection<SerieEntrenamiento> Sets,
        string SourceFingerprint,
        bool RepeatedUnderperformance,
        bool ExcessiveEffort);

    public static class LiveCoachPerformanceTrendDetector
    {
        public static LiveCoachPerformanceTrendEvidence? Detect(SesionEntrenamiento session)
        {
            ArgumentNullException.ThrowIfNull(session);

            var exercise = CurrentPendingTrainingSetResolver.Resolve(session)?.ExerciseSession;
            if (exercise is null)
                return null;

            var lastTwo = exercise.Series
                .Where(set => set.EstaRealmenteCompletada())
                .Where(set => set.NumeroSerie >= 1 && set.NumeroSerie <= exercise.SeriesObjetivo)
                .OrderByDescending(set => set.NumeroSerie)
                .Take(2)
                .OrderBy(set => set.NumeroSerie)
                .ToArray();
            if (lastTwo.Length != 2 || lastTwo[1].NumeroSerie != lastTwo[0].NumeroSerie + 1)
                return null;

            var fingerprint = string.Join('|', lastTwo.Select(set =>
                $"{set.NumeroSerie}:{set.Repeticiones}:{set.Rir}:{set.FechaHoraRegistro.Ticks}"));
            return new(
                exercise,
                lastTwo,
                fingerprint,
                lastTwo.All(set => set.Repeticiones <
                    (set.RepeticionesObjetivoMinimas ?? exercise.RepeticionesMinimasObjetivo)),
                lastTwo.All(set => set.Rir.HasValue && set.Rir.Value <
                    (set.RirObjetivoMinimo ?? exercise.RirObjetivoMinimo)));
        }
    }
}
