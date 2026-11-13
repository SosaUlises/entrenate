using System.Globalization;
using System.Text;
using Entrenate.Domain.Entidades;

namespace Entrenate.Application.Coach.LiveCoach.Rules
{
    public enum LiveCoachExerciseReactivationStatus
    {
        Found,
        Ambiguous,
        AlreadyActive,
        NoOmittedExercises,
        NotFound
    }

    public sealed record LiveCoachExerciseReactivationResult(
        LiveCoachExerciseReactivationStatus Status,
        EjercicioSesion? Exercise = null);

    public static class LiveCoachExerciseReactivationResolver
    {
        private static readonly HashSet<string> CommandWords = new(StringComparer.Ordinal)
        {
            "reactiva", "volve", "volver", "activar", "activa", "quiero", "hacer",
            "recupera", "saca", "omitido", "omitidos", "volvamos", "ejercicio",
            "a", "de", "el", "la", "los", "las", "lo"
        };

        private static readonly HashSet<string> NameStopWords = new(StringComparer.Ordinal)
        {
            "de", "del", "con", "en", "para"
        };

        public static LiveCoachExerciseReactivationResult Resolve(
            SesionEntrenamiento session,
            string requestText)
        {
            var requestedTokens = Tokenize(requestText)
                .Where(token => !CommandWords.Contains(token))
                .ToHashSet(StringComparer.Ordinal);
            if (requestedTokens.Count == 0)
                return new(LiveCoachExerciseReactivationStatus.NotFound);

            var omitted = session.Ejercicios
                .Where(exercise => exercise.Omitido && Matches(exercise, requestedTokens))
                .OrderBy(exercise => exercise.Orden)
                .ToList();
            if (omitted.Count > 1)
                return new(LiveCoachExerciseReactivationStatus.Ambiguous);
            if (omitted.Count == 1)
                return new(LiveCoachExerciseReactivationStatus.Found, omitted[0]);

            var active = session.Ejercicios
                .Where(exercise => !exercise.Omitido && Matches(exercise, requestedTokens))
                .OrderBy(exercise => exercise.Orden)
                .ToList();
            if (active.Count == 1)
                return new(LiveCoachExerciseReactivationStatus.AlreadyActive, active[0]);
            if (active.Count > 1)
                return new(LiveCoachExerciseReactivationStatus.Ambiguous);

            return session.Ejercicios.Any(exercise => exercise.Omitido)
                ? new(LiveCoachExerciseReactivationStatus.NotFound)
                : new(LiveCoachExerciseReactivationStatus.NoOmittedExercises);
        }

        private static bool Matches(
            EjercicioSesion exercise,
            HashSet<string> requestedTokens)
        {
            var nameTokens = Tokenize(exercise.Ejercicio.Nombre)
                .Where(token => !NameStopWords.Contains(token))
                .ToHashSet(StringComparer.Ordinal);
            return requestedTokens.IsSubsetOf(nameTokens) || nameTokens.IsSubsetOf(requestedTokens);
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
