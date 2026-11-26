using System.Globalization;

namespace Entrenate.Application.Coach.LiveCoach.PostSession
{
    internal static class PostSessionDeviationReview
    {
        public const int HighlightLimit = 3;

        public static PostSessionExerciseSummaryDto[] Rank(IEnumerable<PostSessionExerciseSummaryDto> exercises)
            => exercises.Where(item => !item.Omitted && item.AffectedSets > 0)
                .OrderByDescending(item => item.AffectedSets)
                .ThenByDescending(item => (decimal)item.AffectedSets / item.AssessedSets)
                .ThenByDescending(item => item.MaxRepetitionShortfall)
                .ThenByDescending(item => item.ExcessiveEffortSets)
                .ThenByDescending(item => item.MaxRirShortfall)
                .ThenBy(item => item.ExerciseName, StringComparer.Create(CultureInfo.GetCultureInfo("es"), true))
                .ThenBy(item => item.ExerciseSessionId)
                .ToArray();

        public static string DescribePattern(IReadOnlyList<PostSessionExerciseSummaryDto> ranked)
        {
            if (ranked.Count == 0)
                return "No hubo desvíos respecto de los objetivos evaluados.";
            if (ranked.Count == 1)
                return ranked[0].AffectedSets == 1
                    ? "Hubo un desvío aislado en una serie."
                    : $"Hubo desvíos repetidos en un ejercicio ({ranked[0].AffectedSets} series).";

            var repeated = ranked.Count(item => item.AffectedSets > 1);
            return repeated switch
            {
                0 => $"Hubo desvíos aislados en {ranked.Count} ejercicios.",
                1 => $"Hubo desvíos en {ranked.Count} ejercicios, con más de una serie afectada en uno de ellos.",
                _ => $"Hubo desvíos en {ranked.Count} ejercicios, con varias series afectadas en {repeated} de ellos."
            };
        }
    }
}
