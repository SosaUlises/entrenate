using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Entrenate.Application.Common.Interfaces;
using Entrenate.Application.TrainingSessions;
using Entrenate.Domain.Entidades;

namespace Entrenate.Application.Coach.LiveCoach.Rules
{
    public static class LiveCoachPerformanceQueryResolver
    {
        public static string ResolvePreviousSetPerformance(SesionEntrenamiento session)
        {
            var activeExercise = ResolveActiveExercise(session);
            if (activeExercise == null)
            {
                return "No hay un ejercicio activo en la sesión.";
            }

            var completedSets = activeExercise.Series
                .Where(s => s.EstaRealmenteCompletada())
                .OrderBy(s => s.NumeroSerie)
                .ToList();

            if (completedSets.Count == 0)
            {
                return "Todavía no completaste ninguna serie de este ejercicio.";
            }

            var lastSet = completedSets.Last();
            var weight = lastSet.Peso.ToString("0.##", CultureInfo.InvariantCulture);
            var reps = lastSet.Repeticiones == 1 ? "1 repetición" : $"{lastSet.Repeticiones} repeticiones";
            var rirPart = lastSet.Rir.HasValue ? $", RIR {lastSet.Rir.Value}" : string.Empty;

            return $"En la serie anterior hiciste {weight} kg por {reps}{rirPart}.";
        }

        public static string ResolvePreviousSetRir(SesionEntrenamiento session)
        {
            var activeExercise = ResolveActiveExercise(session);
            if (activeExercise == null)
            {
                return "No hay un ejercicio activo en la sesión.";
            }

            var completedSets = activeExercise.Series
                .Where(s => s.EstaRealmenteCompletada())
                .OrderBy(s => s.NumeroSerie)
                .ToList();

            if (completedSets.Count == 0)
            {
                return "Todavía no completaste ninguna serie de este ejercicio.";
            }

            var lastSet = completedSets.Last();
            if (lastSet.Rir.HasValue)
            {
                return $"En la serie anterior registraste RIR {lastSet.Rir.Value}.";
            }

            return "En la serie anterior no registraste RIR.";
        }

        public static string ResolveRemainingSets(SesionEntrenamiento session)
        {
            var activeExercise = ResolveActiveExercise(session);
            if (activeExercise == null)
            {
                return "No hay un ejercicio activo en la sesión.";
            }

            var completedCount = activeExercise.Series.Count(s => s.EstaRealmenteCompletada());
            var remaining = Math.Max(0, activeExercise.SeriesObjetivo - completedCount);

            if (remaining == 0)
            {
                return "Ya completaste todas las series de este ejercicio.";
            }

            if (remaining == 1)
            {
                return "Te falta 1 serie de este ejercicio.";
            }

            return $"Te faltan {remaining} series de este ejercicio.";
        }

        public static string ResolveBestSetToday()
        {
            return "¿Querés comparar por peso, repeticiones o volumen?";
        }

        public static async Task<string> ResolvePreviousExercisePerformanceAsync(
            SesionEntrenamiento session,
            string userId,
            ITrainingSessionRepository sessionRepository,
            CancellationToken cancellationToken = default)
        {
            var activeExercise = ResolveActiveExercise(session);
            if (activeExercise == null)
            {
                return "No hay un ejercicio activo en la sesión.";
            }

            if (activeExercise.EjercicioId == Guid.Empty)
            {
                return "No tengo una sesión anterior registrada para este ejercicio.";
            }

            var exposures = await sessionRepository.GetRecentCompletedExerciseExposuresByUserIdAsync(
                activeExercise.EjercicioId,
                userId,
                cancellationToken);

            if (exposures == null || exposures.Count == 0)
            {
                return "No tengo una sesión anterior registrada para este ejercicio.";
            }

            var latest = exposures
                .OrderByDescending(e => e.StartedAt)
                .FirstOrDefault();

            if (latest == null || latest.CompletedSets == null || latest.CompletedSets.Count == 0)
            {
                return "No tengo una sesión anterior registrada para este ejercicio.";
            }

            var formattedSets = latest.CompletedSets
                .OrderBy(s => s.SetNumber)
                .Select(s => $"{s.Weight.ToString("0.##", CultureInfo.InvariantCulture)} kg × {s.Repetitions}")
                .ToList();

            if (formattedSets.Count == 0)
            {
                return "No tengo una sesión anterior registrada para este ejercicio.";
            }

            if (formattedSets.Count == 1)
            {
                return $"La última vez hiciste {formattedSets[0]}.";
            }

            var allExceptLast = string.Join(", ", formattedSets.Take(formattedSets.Count - 1));
            return $"La última vez hiciste {allExceptLast} y {formattedSets.Last()}.";
        }

        public static string ResolveCompareWithPreviousSession()
        {
            return "¿Querés comparar peso, repeticiones o volumen?";
        }

        public static string ResolveNextExercise(SesionEntrenamiento session)
        {
            var exercises = session.Ejercicios.OrderBy(e => e.Orden).ToList();
            if (exercises.Count == 0)
            {
                return "No hay ejercicios en la sesión.";
            }

            var activeExercise = ResolveActiveExercise(session);
            if (activeExercise == null)
            {
                return "Este es el último ejercicio de la sesión.";
            }

            var currentIndex = exercises.FindIndex(e => e.Id == activeExercise.Id);
            if (currentIndex < 0 || currentIndex >= exercises.Count - 1)
            {
                return "Este es el último ejercicio de la sesión.";
            }

            var currentName = activeExercise.Ejercicio?.Nombre ?? "este ejercicio";
            var nextName = exercises[currentIndex + 1].Ejercicio?.Nombre ?? "el siguiente ejercicio";

            return $"Después de {currentName} sigue {nextName}.";
        }

        public static string ResolveRemainingExercises(SesionEntrenamiento session)
        {
            var exercises = session.Ejercicios.OrderBy(e => e.Orden).ToList();
            if (exercises.Count == 0)
            {
                return "No hay ejercicios en la sesión.";
            }

            var activeExercise = ResolveActiveExercise(session);
            if (activeExercise == null)
            {
                return "Este es el último ejercicio de la sesión.";
            }

            var currentIndex = exercises.FindIndex(e => e.Id == activeExercise.Id);
            if (currentIndex < 0 || currentIndex >= exercises.Count - 1)
            {
                return "Este es el último ejercicio de la sesión.";
            }

            var remaining = exercises.Count - 1 - currentIndex;
            if (remaining == 1)
            {
                return "Te falta 1 ejercicio después de este.";
            }

            return $"Te faltan {remaining} ejercicios después de este.";
        }

        public static string ResolveSessionProgress(SesionEntrenamiento session)
        {
            var exercises = session.Ejercicios.OrderBy(e => e.Orden).ToList();
            if (exercises.Count == 0)
            {
                return "No hay ejercicios en la sesión.";
            }

            var activeExercise = ResolveActiveExercise(session);
            if (activeExercise == null)
            {
                return "Ya completaste todas las series de la sesión.";
            }

            var currentIndex = exercises.FindIndex(e => e.Id == activeExercise.Id);
            var remainingExercises = currentIndex >= 0 ? Math.Max(0, exercises.Count - 1 - currentIndex) : 0;
            var completedCount = activeExercise.Series.Count(s => s.EstaRealmenteCompletada());
            var remainingSets = Math.Max(0, activeExercise.SeriesObjetivo - completedCount);

            if (remainingSets == 0 && remainingExercises == 0)
            {
                return "Ya completaste todas las series de la sesión.";
            }

            if (remainingSets > 0 && remainingExercises == 0)
            {
                if (remainingSets == 1)
                {
                    return "Te falta 1 serie de este ejercicio para terminar la sesión.";
                }

                return $"Te faltan {remainingSets} series de este ejercicio para terminar la sesión.";
            }

            if (remainingSets == 0 && remainingExercises > 0)
            {
                if (remainingExercises == 1)
                {
                    return "Ya completaste este ejercicio. Te falta 1 ejercicio más.";
                }

                return $"Ya completaste este ejercicio. Te faltan {remainingExercises} ejercicios más.";
            }

            var setPart = remainingSets == 1 ? "1 serie" : $"{remainingSets} series";
            var exPart = remainingExercises == 1 ? "1 ejercicio más" : $"{remainingExercises} ejercicios más";
            var verb = remainingSets == 1 ? "Te falta" : "Te faltan";

            return $"{verb} {setPart} de este ejercicio y {exPart}.";
        }

        public static EjercicioSesion? ResolveActiveExercise(SesionEntrenamiento session)
        {
            var currentPending = CurrentPendingTrainingSetResolver.Resolve(session);
            if (currentPending != null)
            {
                return currentPending.ExerciseSession;
            }

            return session.Ejercicios.OrderBy(e => e.Orden).LastOrDefault();
        }
    }
}

