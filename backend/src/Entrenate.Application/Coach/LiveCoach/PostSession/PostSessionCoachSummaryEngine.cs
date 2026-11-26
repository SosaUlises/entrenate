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
            var completedSets = exercise.Series
                .Where(set => set.EstaRealmenteCompletada())
                .GroupBy(set => set.NumeroSerie)
                .Select(group => group.OrderByDescending(set => set.FechaHoraRegistro).First())
                .ToArray();
            var targetSetCount = completedSets.Count(IsPlannedSet);
            var summary = new PostSessionExerciseSummaryDto(
                exercise.Id,
                exercise.Ejercicio.Nombre,
                !exercise.Omitido && targetSetCount == exercise.SeriesObjetivo,
                exercise.Omitido,
                targetSetCount,
                0, 0, 0, 0, 0, 0, 0);
            if (exercise.Omitido)
                return summary;

            // Older planned sets can lack snapshots. Extra sets need recorded targets to be assessed.
            var evidence = completedSets.Select(set =>
            {
                var planned = IsPlannedSet(set);
                int? repMin = set.RepeticionesObjetivoMinimas ?? (planned ? exercise.RepeticionesMinimasObjetivo : null);
                int? repMax = set.RepeticionesObjetivoMaximas ?? (planned ? exercise.RepeticionesMaximasObjetivo : null);
                int? rirMin = set.RirObjetivoMinimo ?? (planned ? exercise.RirObjetivoMinimo : null);
                var repShortfall = repMin is int min ? Math.Max(0, min - set.Repeticiones) : 0;
                var rirShortfall = rirMin is int target && set.Rir is int rir ? Math.Max(0, target - rir) : 0;
                return (
                    Assessed: repMin.HasValue || (rirMin.HasValue && set.Rir.HasValue),
                    RepShortfall: repShortfall,
                    RirShortfall: rirShortfall,
                    RepTargetMet: repMin <= set.Repeticiones && set.Repeticiones <= repMax);
            }).Where(set => set.Assessed).ToArray();

            return summary with
            {
                MissedRepetitionSets = evidence.Count(set => set.RepShortfall > 0),
                ExcessiveEffortSets = evidence.Count(set => set.RirShortfall > 0),
                AssessedSets = evidence.Length,
                AffectedSets = evidence.Count(set => set.RepShortfall > 0 || set.RirShortfall > 0),
                MaxRepetitionShortfall = evidence.Select(set => set.RepShortfall).DefaultIfEmpty().Max(),
                MaxRirShortfall = evidence.Select(set => set.RirShortfall).DefaultIfEmpty().Max(),
                RepetitionTargetMetSets = evidence.Count(set => set.RepTargetMet)
            };

            bool IsPlannedSet(SerieEntrenamiento set) => set.NumeroSerie >= 1 && set.NumeroSerie <= exercise.SeriesObjetivo;
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
            var ranked = PostSessionDeviationReview.Rank(exercises);
            if (ranked.Length > 0)
            {
                var names = string.Join(", ", ranked.Take(PostSessionDeviationReview.HighlightLimit).Select(item => item.ExerciseName));
                yield return $"{PostSessionDeviationReview.DescribePattern(ranked)} " +
                    (ranked.Length > PostSessionDeviationReview.HighlightLimit ? $"Entre ellos: {names}." : $"Ejercicios: {names}.");
            }

            var prescription = adaptations.FirstOrDefault(item => item.Type == "PrescriptionAdaptation");
            if (prescription?.Detail is not null)
                yield return prescription.Detail;
            if (exercises.Any(item => item.Omitted))
                yield return $"Omitiste {FormatExercises(exercises.Count(item => item.Omitted))}; se reportan por separado del rendimiento.";
            if (ranked.Length == 0)
                yield return exercises.Any(item => item.AssessedSets > 0)
                    ? PostSessionDeviationReview.DescribePattern(ranked)
                    : "No hay series evaluables contra la prescripción.";
        }

        private static string FormatExercises(int count) => count == 1 ? "1 ejercicio" : $"{count} ejercicios";
    }
}
