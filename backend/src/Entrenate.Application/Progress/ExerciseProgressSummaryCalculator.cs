using Entrenate.Application.Progress.DTOs;

namespace Entrenate.Application.Progress
{
    internal static class ExerciseProgressSummaryCalculator
    {
        public static ExerciseProgressSummaryDto Calculate(
            IReadOnlyCollection<ExerciseProgressEntryDto> entries)
        {
            var validValues = entries
                .OrderBy(x => x.HoraInicio)
                .ThenBy(x => x.SessionId)
                .Where(x => x.E1RmEstimado.HasValue)
                .Select(x => x.E1RmEstimado!.Value)
                .ToList();

            if (validValues.Count == 0)
            {
                return new ExerciseProgressSummaryDto(
                    null,
                    null,
                    null,
                    null,
                    null);
            }

            var first = validValues.First();
            var last = validValues.Last();
            var best = validValues.Max();

            if (validValues.Count == 1)
            {
                return new ExerciseProgressSummaryDto(
                    first,
                    last,
                    best,
                    null,
                    null);
            }

            var absoluteChange = Math.Round(last - first, 2);
            var percentageChange = first == 0
                ? (decimal?)null
                : Math.Round(
                    (last - first) / first * 100m,
                    2);

            return new ExerciseProgressSummaryDto(
                first,
                last,
                best,
                absoluteChange,
                percentageChange);
        }
    }
}
