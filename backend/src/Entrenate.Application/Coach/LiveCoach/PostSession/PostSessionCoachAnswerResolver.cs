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
            return $"Completaste {summary.CompletedExercises} de {summary.TotalExercises} ejercicios y {summary.CompletedSets} series. " +
                (summary.OmittedExercises > 0 ? $"Omitiste {summary.OmittedExercises}. " : string.Empty) +
                (summary.NotableObservations.FirstOrDefault() ?? "No hubo desvíos relevantes respecto de la prescripción.");
        }

        private static string ResolvePositive(PostSessionCoachSummaryDto summary)
        {
            var positive = summary.Exercises.Where(item => item.Completed && !item.Omitted &&
                item.MissedRepetitionSets == 0 && item.ExcessiveEffortSets == 0)
                .Select(item => item.ExerciseName).ToArray();
            return positive.Length == 0
                ? "No hubo un ejercicio completo claramente dentro de todos los objetivos registrados."
                : $"Completaste dentro de la prescripción: {string.Join(", ", positive)}.";
        }

        private static string ResolveWorst(PostSessionCoachSummaryDto summary)
        {
            var worst = summary.Exercises.OrderByDescending(item => item.MissedRepetitionSets)
                .ThenByDescending(item => item.ExcessiveEffortSets).FirstOrDefault();
            if (worst is null || (worst.MissedRepetitionSets == 0 && worst.ExcessiveEffortSets == 0))
                return "No hubo un ejercicio claramente por debajo de la prescripción.";
            if (worst.MissedRepetitionSets > 0)
                return $"{worst.ExerciseName} tuvo {worst.MissedRepetitionSets} series por debajo del rango de repeticiones.";
            return $"{worst.ExerciseName} tuvo {worst.ExcessiveEffortSets} series con un RIR menor al objetivo.";
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
            var worst = summary.Exercises.OrderByDescending(item => item.MissedRepetitionSets)
                .ThenByDescending(item => item.ExcessiveEffortSets).FirstOrDefault();
            if (worst is null || (worst.MissedRepetitionSets == 0 && worst.ExcessiveEffortSets == 0))
                return "La próxima vez, observá si volvés a mantenerte dentro de los rangos de repeticiones y RIR.";
            return worst.MissedRepetitionSets > 0
                ? $"En {worst.ExerciseName} quedaste por debajo del rango en {worst.MissedRepetitionSets} series; observá si vuelve a pasar en la próxima sesión."
                : $"En {worst.ExerciseName} el RIR quedó por debajo del objetivo en {worst.ExcessiveEffortSets} series; observá si vuelve a pasar.";
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
