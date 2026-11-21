using System.Globalization;
using System.Text;
using Entrenate.Application.Coach.LiveCoach.Clarifications;
using Entrenate.Application.Coach.LiveCoach.Routing;
using Entrenate.Application.Coach.LiveCoach.Rules;

namespace Entrenate.Application.Coach.LiveCoach.Conversation
{
    public enum LiveCoachSessionFollowUpKind
    {
        BestSetCriterion,
        RemainingExercises,
        SessionProgress,
        NavigateToReferencedExercise,
        AdaptationChanges,
        ReferencedExerciseCompletion,
        AmbiguousPerformanceComparison
    }

    public sealed record LiveCoachSessionFollowUp(
        LiveCoachSessionFollowUpKind Kind,
        LiveCoachBestSetCriterion? BestSetCriterion = null);

    public static class LiveCoachSessionFollowUpResolver
    {
        public static bool IsPotentialFollowUp(string question)
        {
            var text = Normalize(question);
            return text.StartsWith("y ", StringComparison.Ordinal) ||
                text is "volve a ese" or "volveme a ese" or "mostrame ese de nuevo" or
                    "mostrame ese otra vez" or "volve al que te dije antes" or
                    "volveme al que te dije antes" or "que sacaste" or "que ajustaste" or
                    "cuales sacaste" or "que ejercicios sacaste" or
                    "ese ejercicio ya lo hice" or "ya hice ese ejercicio" or "ese ya lo hice";
        }

        public static LiveCoachSessionFollowUp? Resolve(
            string question,
            LiveCoachSessionConversationContext context)
        {
            var text = Normalize(question);

            var criterionText = text.StartsWith("y ", StringComparison.Ordinal)
                ? text[2..]
                : text;
            var criterion = LiveCoachBestSetClarificationResolver.ParseCriterion(criterionText);
            if (criterion.HasValue &&
                context.LastReferencedExerciseSessionId.HasValue &&
                (context.LastBestSetCriterion.HasValue ||
                 context.LastIntentKind == LiveCoachIntentKind.BestSetToday))
            {
                return new(LiveCoachSessionFollowUpKind.BestSetCriterion, criterion);
            }

            if ((text is "y ejercicios" or "y ejercicio" or "y cuantos ejercicios" or
                 "y cuantos ejercicios me quedan") &&
                context.LastIntentKind is (LiveCoachIntentKind.RemainingSets or
                    LiveCoachIntentKind.CurrentExerciseProgress or
                    LiveCoachIntentKind.SessionProgress))
            {
                return new(LiveCoachSessionFollowUpKind.RemainingExercises);
            }

            if ((text.Contains("cuanto me falta para terminar", StringComparison.Ordinal) ||
                 text.Contains("como vengo ahora", StringComparison.Ordinal) ||
                 text is "y cuanto me queda" or "cuanto me queda") &&
                context.LastIntentKind is (LiveCoachIntentKind.RemainingSets or
                    LiveCoachIntentKind.RemainingExercises or
                    LiveCoachIntentKind.SessionProgress))
            {
                return new(LiveCoachSessionFollowUpKind.SessionProgress);
            }

            if ((text is "volve a ese" or "volveme a ese" or "mostrame ese de nuevo" or
                 "mostrame ese otra vez" or "volve al que te dije antes" or
                 "volveme al que te dije antes") && context.LastReferencedExerciseSessionId.HasValue)
            {
                return new(LiveCoachSessionFollowUpKind.NavigateToReferencedExercise);
            }

            if ((text is "que sacaste" or "que ajustaste" or "cuales sacaste" or
                 "que ejercicios sacaste") && context.LastAdaptation is not null)
            {
                return new(LiveCoachSessionFollowUpKind.AdaptationChanges);
            }

            if ((text is "ese ejercicio ya lo hice" or "ya hice ese ejercicio" or
                 "ese ya lo hice") && context.LastReferencedExerciseSessionId.HasValue)
            {
                return new(LiveCoachSessionFollowUpKind.ReferencedExerciseCompletion);
            }

            if ((text.Contains("comparado con la anterior", StringComparison.Ordinal) ||
                 text.Contains("comparada con la anterior", StringComparison.Ordinal)) &&
                context.LastPerformanceQueryKind.HasValue)
            {
                return new(LiveCoachSessionFollowUpKind.AmbiguousPerformanceComparison);
            }

            return null;
        }

        private static string Normalize(string value)
        {
            var decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(decomposed.Length);
            foreach (var character in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                    continue;
                builder.Append(char.IsLetterOrDigit(character) ? character : ' ');
            }

            return string.Join(' ', builder.ToString()
                .Normalize(NormalizationForm.FormC)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
