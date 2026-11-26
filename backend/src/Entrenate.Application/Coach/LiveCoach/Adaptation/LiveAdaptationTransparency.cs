using Entrenate.Application.TrainingSessions;
using Entrenate.Application.TrainingSessions.DTOs;
using Entrenate.Domain.Entidades;

namespace Entrenate.Application.Coach.LiveCoach.Adaptation
{
    public sealed record LiveAdaptationTarget(int RepMin, int RepMax, int RirMin, int RirMax);

    public sealed record LiveAdaptationPrescriptionChange(
        string ExerciseName, int FromSet, LiveAdaptationTarget Before, LiveAdaptationTarget After);

    public sealed record LiveAdaptationExercisePreview(
        Guid Id, string Name, string Status, int PendingSets,
        LiveAdaptationTarget Target, int RestSeconds);

    public sealed record LiveAdaptationPreview(
        string Title, string Explanation, int PendingSets,
        int? MinutesBefore, int? MinutesAfter,
        IReadOnlyCollection<LiveAdaptationExercisePreview> Exercises,
        LiveAdaptationPrescriptionChange? PrescriptionChange);

    public static class LiveAdaptationTransparency
    {
        public static LiveAdaptationPreview? Preview(
            SesionEntrenamiento session,
            ILiveAdaptationProposal? proposal,
            LivePrescriptionAdaptationProposal? prescription)
        {
            if (proposal is null && prescription is null)
                return null;

            var omitted = proposal?.Changes
                .Where(change => change.Type == LiveAdaptationChangeType.OmitExercise)
                .Select(change => change.ExerciseSessionId).ToHashSet() ?? [];
            var change = prescription is null ? null : new LiveAdaptationPrescriptionChange(
                prescription.ExerciseName, prescription.EffectiveFromSetNumber,
                new(prescription.PreviousRepMin, prescription.PreviousRepMax,
                    prescription.PreviousRirMin, prescription.PreviousRirMax),
                new(prescription.NewRepMin, prescription.NewRepMax,
                    prescription.NewRirMin, prescription.NewRirMax));

            var exercises = session.Ejercicios.OrderBy(exercise => exercise.Orden).Select(exercise =>
            {
                var pending = LiveTimeConstraintAdaptationEngine.CountPendingSets(exercise);
                var status = exercise.Omitido ? "alreadyOmitted"
                    : omitted.Contains(exercise.Id) ? "newlyOmitted"
                    : pending == 0 ? "completed" : "retained";
                return new LiveAdaptationExercisePreview(
                    exercise.Id, exercise.Ejercicio.Nombre, status,
                    status == "retained" ? pending : 0,
                    exercise.Id == prescription?.ExerciseSessionId && change is not null
                        ? change.After : Target(exercise),
                    exercise.DescansoObjetivoSegundos);
            }).ToArray();
            (int? before, int? after) = proposal switch
            {
                LiveTimeConstraintProposal time => (time.EstimatedMinutesBefore, time.EstimatedMinutesAfter),
                LiveCombinedAdaptationProposal combined => (combined.EstimatedMinutesBefore, combined.EstimatedMinutesAfter),
                null => ((int?)null, (int?)null),
                _ => (LiveTimeConstraintAdaptationEngine.EstimatePendingMinutes(session),
                    LiveTimeConstraintAdaptationEngine.EstimatePendingMinutes(session, omitted))
            };
            var explanation = proposal switch
            {
                LiveTimeConstraintProposal time =>
                    $"Con {time.RequestedRemainingMinutes} minutos disponibles, propongo recortar trabajo pendiente y conservar el ejercicio actual.",
                LiveFatigueAdaptationProposal =>
                    "Por el cansancio que informaste, propongo reducir el trabajo pendiente y conservar el ejercicio actual.",
                LivePerformanceAdaptationProposal performance => DescribeEvidence(performance.Evidence) +
                    " Propongo recortar trabajo posterior.",
                LiveCombinedAdaptationProposal combined => DescribeCombined(combined),
                _ when prescription?.SignalType == LivePerformanceSignalType.ExcessiveEffort =>
                    "En las últimas dos series registraste un RIR menor al objetivo. Propongo ajustar el margen de las series pendientes.",
                _ => "En las últimas dos series quedaste por debajo del objetivo de repeticiones. Propongo ajustar el rango de las series pendientes."
            };
            var title = proposal is LiveTimeConstraintProposal
                ? $"Ajustar el trabajo pendiente a ~{after} min"
                : prescription is not null ? "Ajustar la prescripción pendiente" : "Reducir el trabajo pendiente";
            return new(title, explanation, exercises.Sum(exercise => exercise.PendingSets),
                before, after, exercises, change);
        }

        public static string AppliedOmissions(
            SesionEntrenamiento session, TrainingSessionDto updated, IReadOnlyCollection<Guid> omittedIds)
        {
            var names = session.Ejercicios.Where(exercise => exercise.Omitido && omittedIds.Contains(exercise.Id))
                .OrderBy(exercise => exercise.Orden).Select(exercise => Name(updated, exercise)).ToArray();
            return $"Listo. Omití {string.Join(", ", names)}. {DescribeRemaining(session, updated)} " +
                $"Tiempo pendiente estimado: {LiveTimeConstraintAdaptationEngine.EstimatePendingMinutes(session)} min.";
        }

        public static string AppliedPrescription(
            SesionEntrenamiento session, TrainingSessionDto updated, Guid exerciseId, int fromSet)
        {
            var exercise = session.Ejercicios.Single(item => item.Id == exerciseId);
            return $"Listo. En {Name(updated, exercise)}, el objetivo desde la serie {fromSet} quedó en " +
                $"{DescribeTarget(Target(exercise))}. {DescribeRemaining(session, updated)}";
        }

        private static string Name(TrainingSessionDto updated, EjercicioSesion exercise)
            => updated.Ejercicios.FirstOrDefault(item => item.Id == exercise.Id)?.Nombre
                ?? exercise.Ejercicio.Nombre;

        private static string DescribeRemaining(SesionEntrenamiento session, TrainingSessionDto updated)
        {
            var next = CurrentPendingTrainingSetResolver.Resolve(session);
            if (next is null)
                return "No quedan series pendientes. Podés finalizar el entrenamiento.";

            var pending = session.Ejercicios.Where(exercise => !exercise.Omitido)
                .Sum(LiveTimeConstraintAdaptationEngine.CountPendingSets);
            var count = pending == 1 ? "Queda 1 serie" : $"Quedan {pending} series";
            return $"{count}. Sigue {Name(updated, next.ExerciseSession)}, serie {next.SetNumber}, " +
                $"{DescribeTarget(Target(next.ExerciseSession))}.";
        }

        private static LiveAdaptationTarget Target(EjercicioSesion exercise) => new(
            exercise.RepeticionesMinimasObjetivo, exercise.RepeticionesMaximasObjetivo,
            exercise.RirObjetivoMinimo, exercise.RirObjetivoMaximo);

        private static string DescribeTarget(LiveAdaptationTarget target)
            => $"{Range(target.RepMin, target.RepMax)} repeticiones y RIR {Range(target.RirMin, target.RirMax)}";

        private static string Range(int min, int max) => min == max ? $"{min}" : $"{min} a {max}";

        private static string DescribeCombined(LiveCombinedAdaptationProposal proposal)
        {
            var signals = new List<string>();
            if (proposal.Signals.RemainingMinutes is int minutes)
                signals.Add($"{minutes} minutos disponibles");
            if (proposal.Signals.FatigueSeverity.HasValue)
                signals.Add("el cansancio que informaste");
            var evidence = proposal.PerformanceEvidence is { } performance &&
                proposal.Reasons.Contains(LiveAdaptationReason.Performance)
                ? $" {DescribeEvidence(performance)}"
                : "";
            var context = signals.Count == 0 ? "" : $" Consideré {string.Join(" y ", signals)}.";
            return $"Propongo un solo recorte del trabajo pendiente.{context}{evidence}";
        }

        private static string DescribeEvidence(LivePerformanceEvidence evidence)
        {
            var parts = new List<string>();
            if (evidence.MissedPrescription)
                parts.Add($"{evidence.ActualRepetitions} repeticiones para un objetivo de {Range(evidence.TargetMinimumRepetitions, evidence.TargetMaximumRepetitions)}");
            if (evidence.ExceededTargetEffort && evidence.ActualRir is int rir)
                parts.Add($"RIR {rir} para un objetivo de {Range(evidence.TargetMinimumRir, evidence.TargetMaximumRir)}");
            return parts.Count > 0 ? $"En la última serie registraste {string.Join(" y ", parts)}."
                : "Consideré el rendimiento registrado en la última serie.";
        }
    }
}
