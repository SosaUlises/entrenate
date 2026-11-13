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
        AlreadyVisible,
        NoNextMatch,
        UnknownMuscleGroup,
        Ambiguous,
        NotFound
    }

    public sealed record LiveCoachExerciseNavigationResult(
        LiveCoachExerciseNavigationStatus Status,
        EjercicioSesion? Exercise = null,
        string? MuscleGroupLabel = null);

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

        private static readonly IReadOnlyDictionary<string, MuscleGroupMapping> MuscleGroups =
            new Dictionary<string, MuscleGroupMapping>(StringComparer.Ordinal)
            {
                ["pecho"] = new("pecho", ["pecho"]),
                ["espalda"] = new("espalda", ["espalda"]),
                ["pierna"] = new("piernas", ["cuadriceps", "isquiotibiales", "gluteos", "gemelos"]),
                ["piernas"] = new("piernas", ["cuadriceps", "isquiotibiales", "gluteos", "gemelos"]),
                ["hombro"] = new("hombros", ["hombros"]),
                ["hombros"] = new("hombros", ["hombros"]),
                ["brazo"] = new("brazos", ["biceps", "triceps"]),
                ["brazos"] = new("brazos", ["biceps", "triceps"]),
                ["biceps"] = new("bíceps", ["biceps"]),
                ["triceps"] = new("tríceps", ["triceps"]),
                ["gluteo"] = new("glúteos", ["gluteos"]),
                ["gluteos"] = new("glúteos", ["gluteos"]),
                ["femoral"] = new("femorales", ["isquiotibiales"]),
                ["femorales"] = new("femorales", ["isquiotibiales"]),
                ["isquiotibial"] = new("femorales", ["isquiotibiales"]),
                ["isquiotibiales"] = new("femorales", ["isquiotibiales"]),
                ["cuadriceps"] = new("cuádriceps", ["cuadriceps"]),
                ["pantorrilla"] = new("pantorrillas", ["gemelos"]),
                ["pantorrillas"] = new("pantorrillas", ["gemelos"]),
                ["gemelo"] = new("pantorrillas", ["gemelos"]),
                ["gemelos"] = new("pantorrillas", ["gemelos"]),
                ["abdomen"] = new("core", ["core"]),
                ["abdominales"] = new("core", ["core"]),
                ["core"] = new("core", ["core"])
            };

        private sealed record MuscleGroupMapping(string Label, HashSet<string> Categories);

        public static LiveCoachExerciseNavigationResult ResolveBoundary(
            SesionEntrenamiento session,
            Guid? visibleExerciseSessionId,
            bool first)
        {
            var ordered = session.Ejercicios.OrderBy(exercise => exercise.Orden).ToList();
            if (ordered.Count == 0)
                return new(LiveCoachExerciseNavigationStatus.NotFound);

            var target = first ? ordered[0] : ordered[^1];
            if (visibleExerciseSessionId == target.Id)
            {
                return new(
                    first
                        ? LiveCoachExerciseNavigationStatus.FirstExercise
                        : LiveCoachExerciseNavigationStatus.LastExercise,
                    target);
            }

            return new(LiveCoachExerciseNavigationStatus.Found, target);
        }

        public static LiveCoachExerciseNavigationResult ResolveByMuscleGroup(
            SesionEntrenamiento session,
            string requestText,
            Guid? visibleExerciseSessionId,
            bool nextOnly)
        {
            var mapping = ResolveMuscleGroup(requestText);
            if (mapping is null)
                return new(LiveCoachExerciseNavigationStatus.UnknownMuscleGroup);

            var ordered = session.Ejercicios.OrderBy(exercise => exercise.Orden).ToList();
            var visibleIndex = visibleExerciseSessionId.HasValue
                ? ordered.FindIndex(exercise => exercise.Id == visibleExerciseSessionId.Value)
                : -1;

            if (nextOnly && visibleIndex < 0)
            {
                var authoritative = CurrentPendingTrainingSetResolver.Resolve(session)?.ExerciseSession;
                visibleIndex = authoritative is null
                    ? -1
                    : ordered.FindIndex(exercise => exercise.Id == authoritative.Id);
            }

            if (!nextOnly && visibleIndex >= 0 && MatchesMuscleGroup(ordered[visibleIndex], mapping))
            {
                return new(
                    LiveCoachExerciseNavigationStatus.AlreadyVisible,
                    ordered[visibleIndex],
                    mapping.Label);
            }

            var searchStart = nextOnly ? Math.Max(0, visibleIndex + 1) : 0;
            var match = ordered.Skip(searchStart).FirstOrDefault(exercise => MatchesMuscleGroup(exercise, mapping));
            if (match is not null)
                return new(LiveCoachExerciseNavigationStatus.Found, match, mapping.Label);

            return new(
                nextOnly
                    ? LiveCoachExerciseNavigationStatus.NoNextMatch
                    : LiveCoachExerciseNavigationStatus.NotFound,
                MuscleGroupLabel: mapping.Label);
        }

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

        private static MuscleGroupMapping? ResolveMuscleGroup(string requestText)
        {
            foreach (var token in Tokenize(requestText))
            {
                if (MuscleGroups.TryGetValue(token, out var mapping))
                    return mapping;
            }

            return null;
        }

        private static bool MatchesMuscleGroup(EjercicioSesion exercise, MuscleGroupMapping mapping)
        {
            var category = Tokenize(exercise.Ejercicio.GrupoMuscularPrincipal).FirstOrDefault();
            return category is not null && mapping.Categories.Contains(category);
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
