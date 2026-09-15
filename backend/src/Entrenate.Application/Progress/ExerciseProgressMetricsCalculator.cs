using Entrenate.Application.Progress.DTOs;

namespace Entrenate.Application.Progress
{
    internal static class ExerciseProgressMetricsCalculator
    {
        public static ExerciseProgressEntryDto Calculate(
            ExerciseProgressEntryDto entry)
        {
            var completedSets = entry.Series
                .Where(x => x.Completada)
                .ToList();

            var volume = completedSets.Any(x => x.Peso > 0)
                ? completedSets.Sum(x => x.Peso * x.Repeticiones)
                : (decimal?)null;

            var bestSet = completedSets
                .Where(x => x.Peso > 0)
                .Select(x => new
                {
                    Set = x,
                    EstimatedOneRepMax = CalculateEstimatedOneRepMax(
                        x.Peso,
                        x.Repeticiones)
                })
                .OrderByDescending(x => x.EstimatedOneRepMax)
                .ThenByDescending(x => x.Set.Peso)
                .ThenByDescending(x => x.Set.Repeticiones)
                .ThenBy(x => x.Set.NumeroSerie)
                .FirstOrDefault();

            if (bestSet is null)
            {
                return entry with
                {
                    VolumenTotal = volume,
                    MejorSerie = null,
                    E1RmEstimado = null
                };
            }

            return entry with
            {
                VolumenTotal = volume,
                MejorSerie = new BestTrainingSetDto(
                    bestSet.Set.NumeroSerie,
                    bestSet.Set.Peso,
                    bestSet.Set.Repeticiones,
                    bestSet.Set.Rir),
                E1RmEstimado = Math.Round(
                    bestSet.EstimatedOneRepMax,
                    2)
            };
        }

        private static decimal CalculateEstimatedOneRepMax(
            decimal weight,
            int repetitions)
        {
            return weight * (1m + repetitions / 30m);
        }
    }
}
