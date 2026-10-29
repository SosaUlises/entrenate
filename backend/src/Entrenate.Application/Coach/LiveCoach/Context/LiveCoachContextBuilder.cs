using System;
using System.Collections.Generic;
using System.Linq;
using Entrenate.Application.Coach.LiveCoach.Models;
using Entrenate.Domain.Entidades;

namespace Entrenate.Application.Coach.LiveCoach.Context
{
    public sealed record ClientSnapshotInput(
        Guid? ExerciseId,
        int? SetIndex,
        bool? RestIsActive,
        int? RestRemainingSeconds,
        DateTime? RestCapturedAtUtc);

    public interface ILiveCoachContextBuilder
    {
        LiveCoachSessionContext Build(
            SesionEntrenamiento session,
            ClientSnapshotInput? clientSnapshot,
            DateTime nowUtc);
    }

    public sealed class LiveCoachContextBuilder : ILiveCoachContextBuilder
    {
        public LiveCoachSessionContext Build(
            SesionEntrenamiento session,
            ClientSnapshotInput? clientSnapshot,
            DateTime nowUtc)
        {
            var orderedExercises = session.Ejercicios.OrderBy(e => e.Orden).ToList();

            // 1. Authoritative active exercise resolution
            var activeExercise = orderedExercises.FirstOrDefault(e => e.Series.Count < e.SeriesObjetivo)
                ?? orderedExercises.LastOrDefault()
                ?? orderedExercises.FirstOrDefault();

            var isStale = false;
            string? driftNote = null;

            if (activeExercise != null && clientSnapshot != null)
            {
                if (clientSnapshot.ExerciseId.HasValue &&
                    clientSnapshot.ExerciseId.Value != Guid.Empty &&
                    clientSnapshot.ExerciseId.Value != activeExercise.EjercicioId)
                {
                    isStale = true;
                    var name = activeExercise.Ejercicio?.Nombre ?? "ejercicio actual";
                    driftNote = $"Nota de contexto: el usuario avanzó de ejercicio durante la consulta y ahora se encuentra en {name}.";
                }
                else if (clientSnapshot.SetIndex.HasValue &&
                         clientSnapshot.SetIndex.Value != (activeExercise.Series.Count + 1))
                {
                    isStale = true;
                    var currentSet = activeExercise.Series.Count + 1;
                    var name = activeExercise.Ejercicio?.Nombre ?? "ejercicio actual";
                    driftNote = $"Nota de contexto: el usuario completó la serie anterior durante la consulta y ahora se encuentra en la serie {currentSet} de {name}.";
                }
            }

            // 2. Exercise context
            LiveCoachExerciseContext? exerciseContext = null;
            LiveCoachSetContext? setContext = null;
            LiveCoachPrescriptionContext? prescriptionContext = null;

            if (activeExercise != null)
            {
                var exerciseIndex = orderedExercises.IndexOf(activeExercise);
                string? nextExerciseName = null;
                if (exerciseIndex >= 0 && exerciseIndex + 1 < orderedExercises.Count)
                {
                    nextExerciseName = orderedExercises[exerciseIndex + 1].Ejercicio?.Nombre;
                }

                exerciseContext = new LiveCoachExerciseContext(
                    activeExercise.Id,
                    activeExercise.EjercicioId,
                    activeExercise.Ejercicio?.Nombre ?? "Ejercicio",
                    activeExercise.Orden,
                    nextExerciseName);

                var setNumber = activeExercise.Series.Count + 1;
                setContext = new LiveCoachSetContext(
                    setNumber,
                    setNumber <= activeExercise.SeriesObjetivo,
                    false,
                    activeExercise.SeriesObjetivo);

                prescriptionContext = new LiveCoachPrescriptionContext(
                    activeExercise.SeriesObjetivo,
                    activeExercise.RepeticionesMinimasObjetivo,
                    activeExercise.RepeticionesMaximasObjetivo,
                    activeExercise.RirObjetivoMinimo,
                    activeExercise.RirObjetivoMaximo,
                    activeExercise.DescansoObjetivoSegundos,
                    activeExercise.Notas);
            }

            // 3. Completed sets
            var completedSets = new List<LiveCoachCompletedSetContext>();
            foreach (var ex in orderedExercises)
            {
                var exName = ex.Ejercicio?.Nombre ?? "Ejercicio";
                foreach (var s in ex.Series.OrderBy(s => s.NumeroSerie))
                {
                    completedSets.Add(new LiveCoachCompletedSetContext(
                        exName,
                        s.NumeroSerie,
                        s.Peso,
                        s.Repeticiones,
                        s.Rir,
                        s.FechaHoraRegistro));
                }
            }

            // 4. Rest timer calculation
            LiveCoachRestContext restContext;
            if (clientSnapshot?.RestIsActive == true &&
                clientSnapshot.RestRemainingSeconds.HasValue &&
                clientSnapshot.RestCapturedAtUtc.HasValue)
            {
                var elapsed = (nowUtc - clientSnapshot.RestCapturedAtUtc.Value).TotalSeconds;
                var remaining = (int)Math.Max(0, clientSnapshot.RestRemainingSeconds.Value - elapsed);
                var isActive = remaining > 0;

                restContext = new LiveCoachRestContext(
                    isActive,
                    clientSnapshot.RestRemainingSeconds.Value,
                    remaining,
                    nowUtc);
            }
            else
            {
                restContext = new LiveCoachRestContext(
                    false,
                    null,
                    null,
                    nowUtc);
            }

            // 5. Session summary
            var totalTargetSets = orderedExercises.Sum(e => e.SeriesObjetivo);
            var completedTargetSets = orderedExercises.Sum(e => e.Series.Count(s => s.NumeroSerie <= e.SeriesObjetivo));
            var summary = new LiveCoachSessionSummaryContext(
                session.HoraInicio,
                orderedExercises.Count,
                totalTargetSets,
                completedTargetSets);

            return new LiveCoachSessionContext(
                session.Id,
                session.DiaRutinaId,
                nowUtc,
                isStale,
                driftNote,
                exerciseContext,
                setContext,
                prescriptionContext,
                completedSets,
                restContext,
                summary);
        }
    }
}
