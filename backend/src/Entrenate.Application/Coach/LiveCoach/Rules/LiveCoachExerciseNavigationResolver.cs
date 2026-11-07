using System.Globalization;
using System.Text;
using Entrenate.Application.TrainingSessions;
using Entrenate.Domain.Entidades;

namespace Entrenate.Application.Coach.LiveCoach.Rules
{
    public enum LiveCoachExerciseNavigationStatus
    {
        Found,
        FirstExercise,
        LastExercise,
        Ambiguous,
        NotFound
    }

    public sealed record LiveCoachExerciseNavigationResult(
        LiveCoachExerciseNavigationStatus Status,
        EjercicioSesion? Exercise = null);

    public static class LiveCoachExerciseNavigationResolver
    {
        private static readonly HashSet<string> CommandWords = new(StringComparer.Ordinal)
        {
            "abri", "abrir", "mostrame", "mostrar", "ver", "quiero", "llevame",
            "anda", "andame", "navega", "navegame", "ejercicio", "a", "al", "el", "la"
        };

        private static readonly HashSet<string> NameStopWords = new(StringComparer.Ordinal)
        {
            "de", "del", "con", "en", "para"
        };

        public static LiveCoachExerciseNavigationResult ResolveRelative(
            SesionEntrenamiento session,
            Guid? visibleExerciseSessionId,
            int offset)
        {
            var ordered = session.Ejercicios.OrderBy(exercise => exercise.Orden).ToList();
            if (ordered.Count == 0)
                return new(LiveCoachExerciseNavigationStatus.NotFound);

            var visibleIndex = visibleExerciseSessionId.HasValue
                ? ordered.FindIndex(exercise => exercise.Id == visibleExerciseSessionId.Value)
                : -1;
            if (visibleIndex < 0)
            {
                var authoritative = CurrentPendingTrainingSetResolver.Resolve(session)?.ExerciseSession;
                visibleIndex = authoritative is null
                    ? 0
                    : ordered.FindIndex(exercise => exercise.Id == authoritative.Id);
                if (visibleIndex < 0) visibleIndex = 0;
            }

            var targetIndex = visibleIndex + offset;
            if (targetIndex < 0)
                return new(LiveCoachExerciseNavigationStatus.FirstExercise, ordered[visibleIndex]);
            if (targetIndex >= ordered.Count)
                return new(LiveCoachExerciseNavigationStatus.LastExercise, ordered[visibleIndex]);

            return new(LiveCoachExerciseNavigationStatus.Found, ordered[targetIndex]);
        }

        public static LiveCoachExerciseNavigationResult ResolveByName(
            SesionEntrenamiento session,
            string requestText)
        {
            var requestedTokens = Tokenize(requestText)
                .Where(token => !CommandWords.Contains(token))
                .ToHashSet(StringComparer.Ordinal);
            if (requestedTokens.Count == 0)
                return new(LiveCoachExerciseNavigationStatus.NotFound);

            var matches = session.Ejercicios
                .Where(exercise => Matches(exercise, requestedTokens))
                .OrderBy(exercise => exercise.Orden)
                .ToList();

            return matches.Count switch
            {
                1 => new(LiveCoachExerciseNavigationStatus.Found, matches[0]),
                > 1 => new(LiveCoachExerciseNavigationStatus.Ambiguous),
                _ => new(LiveCoachExerciseNavigationStatus.NotFound)
            };
        }

        private static bool Matches(EjercicioSesion exercise, HashSet<string> requestedTokens)
        {
            var nameTokens = Tokenize(exercise.Ejercicio.Nombre)
                .Where(token => !NameStopWords.Contains(token))
                .ToHashSet(StringComparer.Ordinal);
            return requestedTokens.SetEquals(nameTokens) ||
                   requestedTokens.IsSubsetOf(nameTokens) ||
                   nameTokens.IsSubsetOf(requestedTokens);
        }

        private static IEnumerable<string> Tokenize(string value)
        {
            var decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(decomposed.Length);
            foreach (var character in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                    continue;
                builder.Append(char.IsLetterOrDigit(character) ? character : ' ');
            }

            return builder.ToString()
                .Normalize(NormalizationForm.FormC)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
