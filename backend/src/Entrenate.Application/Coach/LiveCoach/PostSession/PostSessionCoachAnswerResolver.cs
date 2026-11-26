using System.Globalization;
using System.Text;

namespace Entrenate.Application.Coach.LiveCoach.PostSession
{
    public static class PostSessionCoachAnswerResolver
    {
        public static string Resolve(PostSessionCoachSummaryDto summary, string question)
        {
            var text = Normalize(question);
            if (text.Contains("salio bien") || text.Contains("fue bien"))
                return ResolvePositive(summary);
            if (text.Contains("rendi peor") || text.Contains("salio peor"))
                return ResolveWorst(summary);
            if (text.Contains("adaptacion") || text.Contains("ajuste") || text.Contains("cambiaste"))
                return ResolveAdaptations(summary);
            if (text.Contains("mejor serie"))
                return ResolveBestSet(summary);
            if (text.Contains("proxima") || text.Contains("deberia mirar") || text.Contains("observar"))
                return ResolveNext(summary);
            var ranked = PostSessionDeviationReview.Rank(summary.Exercises);
            var assessed = summary.Exercises.Sum(item => item.AssessedSets);
            return $"Completaste {summary.CompletedExercises} de {summary.TotalExercises} ejercicios planificados y registraste {summary.CompletedSets} series. " +
                (summary.OmittedExercises > 0 ? $"Ejercicios omitidos: {summary.OmittedExercises}. " : string.Empty) +
                (assessed > 0
                    ? $"Series con desvíos: {ranked.Sum(item => item.AffectedSets)} de {assessed} evaluadas."
                    : "No hay series evaluables contra la prescripción.");
        }

        private static string ResolvePositive(PostSessionCoachSummaryDto summary)
        {
            var positive = summary.Exercises.Where(item => item.RepetitionTargetMetSets > 0)
                .OrderByDescending(item => item.RepetitionTargetMetSets)
                .ThenBy(item => item.ExerciseName, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (positive.Length == 0)
                return "No hay series registradas dentro del rango objetivo de repeticiones.";

            var examples = positive.Take(PostSessionDeviationReview.HighlightLimit)
                .Select(item => $"{item.ExerciseName} ({item.RepetitionTargetMetSets})");
            var total = positive.Sum(item => item.RepetitionTargetMetSets);
            var count = total == 1 ? "1 serie" : $"{total} series";
            return $"Alcanzaste el rango de repeticiones en {count}. " +
                $"Series en rango por ejercicio{(positive.Length > PostSessionDeviationReview.HighlightLimit ? " destacado" : "")}: {string.Join(", ", examples)}.";
        }

        private static string ResolveWorst(PostSessionCoachSummaryDto summary)
        {
            var ranked = PostSessionDeviationReview.Rank(summary.Exercises);
            if (ranked.Length == 0)
                return summary.Exercises.Any(item => item.AssessedSets > 0)
                    ? "No hubo series por debajo del mínimo de repeticiones ni registros por debajo del RIR objetivo."
                    : "No hay series evaluables contra la prescripción.";

            var details = ranked.Take(PostSessionDeviationReview.HighlightLimit).Select(item =>
            {
                var notes = new List<string>();
                if (item.MissedRepetitionSets > 0)
                    notes.Add($"reps por debajo en {item.MissedRepetitionSets}, con un faltante máximo de {item.MaxRepetitionShortfall}");
                if (item.ExcessiveEffortSets > 0)
                    notes.Add($"RIR por debajo en {item.ExcessiveEffortSets}");
                return $"{item.ExerciseName}: {item.AffectedSets} de {item.AssessedSets} series afectadas ({string.Join(" y ", notes)}).";
            });
            var introduction = ranked.Length > PostSessionDeviationReview.HighlightLimit ? " Algunos ejemplos:" : "";
            return $"{PostSessionDeviationReview.DescribePattern(ranked)}{introduction} {string.Join(" ", details)}";
        }

        private static string ResolveAdaptations(PostSessionCoachSummaryDto summary)
        {
            if (summary.AdaptationsApplied.Count == 0)
                return "No se aplicaron adaptaciones durante esta sesión.";

            var labels = string.Join(", ", summary.AdaptationsApplied.Select(item => item.Label.ToLowerInvariant()));
            return summary.AdaptationsApplied.Count == 1
                ? $"Se aplicó 1 ajuste: {labels}."
                : $"Se aplicaron {summary.AdaptationsApplied.Count} ajustes: {labels}.";
        }

        private static string ResolveBestSet(PostSessionCoachSummaryDto summary)
        {
            var best = summary.BestSets.FirstOrDefault(item => item.Criterion == "Volume" && item.Volume > 0)
                ?? summary.BestSets.FirstOrDefault(item => item.Criterion == "Repetitions");
            return best is null
                ? "No hay series completadas para comparar."
                : best.Volume > 0
                    ? $"Por volumen, tu mejor serie fue {best.ExerciseName}: {best.WeightKg:0.##} kg por {best.Repetitions} repeticiones."
                    : $"Por repeticiones, tu mejor serie fue {best.ExerciseName}: {best.Repetitions} repeticiones.";
        }

        private static string ResolveNext(PostSessionCoachSummaryDto summary)
        {
            var ranked = PostSessionDeviationReview.Rank(summary.Exercises);
            if (ranked.Length == 0)
                return "La próxima vez, registrá las repeticiones y el RIR para poder compararlos con sus objetivos.";

            var measure = summary.UnderperformanceEvents == 0 ? "el RIR registrado frente a su mínimo"
                : summary.ExcessiveEffortEvents == 0 ? "las repeticiones frente a su mínimo"
                : "las repeticiones y el RIR registrado frente a sus mínimos";
            if (ranked.Length > 1)
                return $"La próxima vez, compará {measure} en los ejercicios afectados. Fijate si los desvíos vuelven a aparecer en varios ejercicios.";

            var exercise = ranked[0];
            return exercise.AffectedSets == 1
                ? $"La próxima vez, compará {measure} en {exercise.ExerciseName} para ver si se repite ese desvío aislado."
                : $"La próxima vez, compará {measure} en las series de {exercise.ExerciseName} para ver si el patrón se repite.";
        }

        private static string Normalize(string value)
        {
            var decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(decomposed.Length);
            foreach (var character in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                    builder.Append(char.IsLetterOrDigit(character) ? character : ' ');
            }
            return string.Join(' ', builder.ToString().Normalize(NormalizationForm.FormC)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
