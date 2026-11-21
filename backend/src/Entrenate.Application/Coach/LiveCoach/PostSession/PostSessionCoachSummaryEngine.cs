using Entrenate.Domain.Entidades;
using Entrenate.Domain.Enums;

namespace Entrenate.Application.Coach.LiveCoach.PostSession
{
    public static class PostSessionCoachSummaryEngine
    {
        public static PostSessionCoachSummaryDto Create(SesionEntrenamiento session)
        {
            ArgumentNullException.ThrowIfNull(session);
            if (session.Estado != EstadoSesionEntrenamiento.Completada)
                throw new InvalidOperationException("El resumen requiere una sesion completada.");

            var exercises = session.Ejercicios.OrderBy(item => item.Orden)
                .Select(CreateExerciseSummary).ToArray();
            var completedSets = session.Ejercicios
                .SelectMany(item => item.Series.Select(set => (Exercise: item, Set: set)))
                .Where(item => item.Set.EstaRealmenteCompletada())
                .ToArray();
            var adaptations = CreateAdaptations(session).ToArray();
            var bestSets = CreateBestSets(completedSets).ToArray();
            var observations = CreateObservations(exercises, adaptations).Take(3).ToArray();

            return new(
                session.Id,
                exercises.Length,
                exercises.Count(item => item.Completed),
                exercises.Count(item => item.Omitted),
                completedSets.Length,
                exercises.Sum(item => item.MissedRepetitionSets),
                exercises.Sum(item => item.ExcessiveEffortSets),
                adaptations,
                bestSets,
                exercises,
                observations);
        }

        private static PostSessionExerciseSummaryDto CreateExerciseSummary(EjercicioSesion exercise)
        {
            var targetSets = exercise.Series
                .Where(set => set.EstaRealmenteCompletada() &&
                    set.NumeroSerie >= 1 && set.NumeroSerie <= exercise.SeriesObjetivo)
                .GroupBy(set => set.NumeroSerie)
                .Select(group => group.OrderByDescending(set => set.FechaHoraRegistro).First())
                .ToArray();
            var missed = targetSets.Count(set => set.Repeticiones <
                (set.RepeticionesObjetivoMinimas ?? exercise.RepeticionesMinimasObjetivo));
            var effort = targetSets.Count(set => set.Rir.HasValue && set.Rir.Value <
                (set.RirObjetivoMinimo ?? exercise.RirObjetivoMinimo));

            return new(
                exercise.Id,
                exercise.Ejercicio.Nombre,
                targetSets.Length == exercise.SeriesObjetivo,
                exercise.Omitido,
                targetSets.Length,
                missed,
                effort);
        }

        private static IEnumerable<PostSessionAdaptationDto> CreateAdaptations(SesionEntrenamiento session)
        {
            if (session.Adaptacion is not null)
            {
                yield return new("AdaptiveTraining", "Adaptación inicial de la sesión");
            }

            foreach (var adaptation in session.AdaptacionesLiveCoach.OrderBy(item => item.FechaAplicacionUtc))
            {
                var exercise = adaptation.EjercicioSesionId.HasValue
                    ? session.Ejercicios.FirstOrDefault(item => item.Id == adaptation.EjercicioSesionId.Value)
                    : null;
                var (type, label) = adaptation.Tipo switch
                {
                    TipoAdaptacionLiveCoach.Tiempo => ("TimeConstraint", "Recorte por tiempo"),
                    TipoAdaptacionLiveCoach.Fatiga => ("Fatigue", "Ajuste por fatiga informada"),
                    TipoAdaptacionLiveCoach.Rendimiento => ("Performance", "Ajuste por rendimiento"),
                    TipoAdaptacionLiveCoach.Combinada => ("Combined", "Ajuste por señales combinadas"),
                    _ => ("PrescriptionAdaptation", "Ajuste de prescripción")
                };
                string? detail = null;
                if (adaptation.Tipo == TipoAdaptacionLiveCoach.Prescripcion && exercise is not null)
                {
                    var changedReps = adaptation.RepeticionesMinimasAnteriores != adaptation.RepeticionesMinimasNuevas ||
                        adaptation.RepeticionesMaximasAnteriores != adaptation.RepeticionesMaximasNuevas;
                    detail = changedReps
                        ? $"{exercise.Ejercicio.Nombre}: {adaptation.RepeticionesMinimasAnteriores}–{adaptation.RepeticionesMaximasAnteriores} a {adaptation.RepeticionesMinimasNuevas}–{adaptation.RepeticionesMaximasNuevas} reps desde la serie {adaptation.SerieEfectivaDesde}."
                        : $"{exercise.Ejercicio.Nombre}: RIR {adaptation.RirMinimoAnterior}–{adaptation.RirMaximoAnterior} a {adaptation.RirMinimoNuevo}–{adaptation.RirMaximoNuevo} desde la serie {adaptation.SerieEfectivaDesde}.";
                }
                yield return new(type, label, detail);
            }
        }

        private static IEnumerable<PostSessionBestSetDto> CreateBestSets(
            IReadOnlyCollection<(EjercicioSesion Exercise, SerieEntrenamiento Set)> sets)
        {
            if (sets.Count == 0)
                yield break;

            yield return ToBest("Volume", sets.OrderByDescending(item => item.Set.Peso * item.Set.Repeticiones)
                .ThenByDescending(item => item.Set.Peso).ThenByDescending(item => item.Set.Repeticiones).First());
            yield return ToBest("Weight", sets.OrderByDescending(item => item.Set.Peso)
                .ThenByDescending(item => item.Set.Repeticiones).First());
            yield return ToBest("Repetitions", sets.OrderByDescending(item => item.Set.Repeticiones)
                .ThenByDescending(item => item.Set.Peso).First());
        }

        private static PostSessionBestSetDto ToBest(
            string criterion,
            (EjercicioSesion Exercise, SerieEntrenamiento Set) item)
            => new(criterion, item.Exercise.Ejercicio.Nombre, item.Set.NumeroSerie,
                item.Set.Peso, item.Set.Repeticiones, item.Set.Rir,
                item.Set.Peso * item.Set.Repeticiones);

        private static IEnumerable<string> CreateObservations(
            IReadOnlyCollection<PostSessionExerciseSummaryDto> exercises,
            IReadOnlyCollection<PostSessionAdaptationDto> adaptations)
        {
            var worst = exercises.OrderByDescending(item => item.MissedRepetitionSets)
                .ThenByDescending(item => item.ExcessiveEffortSets).FirstOrDefault();
            if (worst is not null && worst.MissedRepetitionSets > 0)
                yield return $"En {worst.ExerciseName} quedaste por debajo del rango objetivo en {FormatSets(worst.MissedRepetitionSets)}.";
            else if (worst is not null && worst.ExcessiveEffortSets > 0)
                yield return $"En {worst.ExerciseName}, {FormatSets(worst.ExcessiveEffortSets)} tuvieron un RIR menor al prescripto.";

            var prescription = adaptations.FirstOrDefault(item => item.Type == "PrescriptionAdaptation");
            if (prescription?.Detail is not null)
                yield return prescription.Detail;
            if (exercises.Any(item => item.Omitted))
                yield return $"Omitiste {FormatExercises(exercises.Count(item => item.Omitted))}; se reportan por separado del rendimiento.";
            if (exercises.All(item => item.MissedRepetitionSets == 0 && item.ExcessiveEffortSets == 0))
                yield return "No hubo desvíos relevantes respecto de la prescripción registrada.";
        }

        private static string FormatSets(int count) => count == 1 ? "1 serie" : $"{count} series";
        private static string FormatExercises(int count) => count == 1 ? "1 ejercicio" : $"{count} ejercicios";
    }
}
