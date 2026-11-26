using Entrenate.Application.Coach.LiveCoach.Adaptation;
using Entrenate.Application.Coach.LiveCoach.Conversation;
using Entrenate.Application.TrainingSessions;
using Entrenate.Domain.Entidades;

namespace Entrenate.Application.Coach.LiveCoach.Proactive
{
    public sealed record LiveCoachProactiveSnapshot(
        Guid? RestExerciseSessionId = null,
        DateTime? RestStartedAtUtc = null);

    public sealed record LiveCoachProactiveCandidate(
        LiveCoachProactiveTriggerType Type,
        Guid? ExerciseSessionId,
        string SourceFingerprint,
        string Text,
        string EvidenceText,
        string? ActionLabel = null,
        string? ActionPrompt = null);

    public static class LiveCoachProactiveSuggestionEngine
    {
        public static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(5);
        public const int LongRestGraceSeconds = 60;
        public const int TimeDriftToleranceMinutes = 2;
        public static readonly TimeSpan TimeConstraintValidity = TimeSpan.FromMinutes(20);

        public static IReadOnlyCollection<LiveCoachProactiveCandidate> Evaluate(
            SesionEntrenamiento session,
            LiveCoachProactiveSnapshot? snapshot,
            LiveCoachTimeConstraintReference? timeConstraint,
            DateTime nowUtc,
            bool interactionBlocked = false)
        {
            ArgumentNullException.ThrowIfNull(session);
            if (interactionBlocked || CurrentPendingTrainingSetResolver.Resolve(session) is null)
                return [];

            var candidates = new List<LiveCoachProactiveCandidate>();
            var performance = EvaluatePerformance(session, nowUtc);
            if (performance.Underperformance is not null)
                candidates.Add(performance.Underperformance);
            if (performance.ExcessiveEffort is not null)
                candidates.Add(performance.ExcessiveEffort);

            var timeDrift = EvaluateTimeDrift(session, timeConstraint, nowUtc);
            if (timeDrift is not null)
                candidates.Add(timeDrift);

            var longRest = EvaluateLongRest(session, snapshot, nowUtc);
            if (longRest is not null)
                candidates.Add(longRest);

            return candidates
                .OrderBy(candidate => GetPriority(candidate.Type))
                .ToArray();
        }

        public static bool CanEmit(
            LiveCoachProactiveCandidate candidate,
            IReadOnlyCollection<LiveCoachProactiveTriggerReference>? recent,
            DateTime nowUtc)
        {
            var previous = recent?
                .Where(item => item.Type == candidate.Type &&
                    item.ExerciseSessionId == candidate.ExerciseSessionId)
                .OrderByDescending(item => item.TriggeredAtUtc)
                .FirstOrDefault();
            if (previous is null)
                return true;
            if (previous.SourceFingerprint == candidate.SourceFingerprint)
                return false;
            return nowUtc - previous.TriggeredAtUtc >= Cooldown;
        }

        private static (LiveCoachProactiveCandidate? Underperformance, LiveCoachProactiveCandidate? ExcessiveEffort)
            EvaluatePerformance(SesionEntrenamiento session, DateTime nowUtc)
        {
            var evidence = LiveCoachPerformanceTrendDetector.Detect(session);
            if (evidence is null)
                return (null, null);

            var exercise = evidence.Exercise;

            bool CanAdjust(LivePerformanceSignalType signal) =>
                LivePrescriptionAdaptationEngine.Evaluate(session, session.UsuarioId, signal, nowUtc, evidence)
                    .Status == LivePrescriptionAdaptationStatus.Proposal;

            var underperformance = evidence.RepeatedUnderperformance && CanAdjust(LivePerformanceSignalType.MissedRepetitions)
                ? new LiveCoachProactiveCandidate(
                    LiveCoachProactiveTriggerType.RepeatedUnderperformance,
                    exercise.Id,
                    evidence.SourceFingerprint,
                    "Las últimas dos series quedaron por debajo de la prescripción. Si querés, puedo ajustar el trabajo que queda.",
                    $"Las últimas dos series de {exercise.Ejercicio.Nombre} quedaron por debajo del mínimo de {exercise.RepeticionesMinimasObjetivo} repeticiones.",
                    "Revisar prescripción",
                    "No llego a las repeticiones en las últimas dos series; revisá la prescripción pendiente.")
                : null;
            var excessiveEffort = evidence.ExcessiveEffort && CanAdjust(LivePerformanceSignalType.ExcessiveEffort)
                ? new LiveCoachProactiveCandidate(
                    LiveCoachProactiveTriggerType.ExcessiveEffort,
                    exercise.Id,
                    evidence.SourceFingerprint,
                    "Las últimas dos series quedaron más cerca del fallo de lo previsto. Puedo revisar lo que queda de la sesión.",
                    $"Las últimas dos series de {exercise.Ejercicio.Nombre} tuvieron un RIR menor al objetivo {exercise.RirObjetivoMinimo}–{exercise.RirObjetivoMaximo}.",
                    "Revisar prescripción",
                    "Este peso está demasiado pesado en las últimas dos series; revisá la prescripción pendiente.")
                : null;
            return (underperformance, excessiveEffort);
        }

        public static bool IsReferenceCurrent(
            SesionEntrenamiento session, LiveCoachProactiveTriggerReference reference, DateTime nowUtc)
        {
            if (CurrentPendingTrainingSetResolver.Resolve(session) is null ||
                reference.PendingWorkFingerprint != LiveTimeConstraintFingerprint.Create(session))
                return false;

            if (reference.Type is LiveCoachProactiveTriggerType.RepeatedUnderperformance or
                LiveCoachProactiveTriggerType.ExcessiveEffort)
            {
                var performance = EvaluatePerformance(session, nowUtc);
                var candidate = reference.Type == LiveCoachProactiveTriggerType.RepeatedUnderperformance
                    ? performance.Underperformance : performance.ExcessiveEffort;
                return candidate is not null && Matches(candidate, reference);
            }

            return true;
        }

        public static bool Matches(LiveCoachProactiveCandidate candidate, LiveCoachProactiveTriggerReference reference)
            => candidate.Type == reference.Type &&
                candidate.ExerciseSessionId == reference.ExerciseSessionId &&
                candidate.SourceFingerprint == reference.SourceFingerprint;

        private static LiveCoachProactiveCandidate? EvaluateLongRest(
            SesionEntrenamiento session,
            LiveCoachProactiveSnapshot? snapshot,
            DateTime nowUtc)
        {
            if (snapshot?.RestStartedAtUtc is null || !snapshot.RestExerciseSessionId.HasValue)
                return null;
            var exercise = session.Ejercicios.FirstOrDefault(item =>
                item.Id == snapshot.RestExerciseSessionId.Value && !item.Omitido);
            if (exercise is null || exercise.DescansoObjetivoSegundos <= 0)
                return null;
            var elapsedSeconds = (nowUtc - snapshot.RestStartedAtUtc.Value).TotalSeconds;
            if (elapsedSeconds < exercise.DescansoObjetivoSegundos + LongRestGraceSeconds)
                return null;

            return new(
                LiveCoachProactiveTriggerType.LongRest,
                exercise.Id,
                $"{exercise.Id:N}:{snapshot.RestStartedAtUtc.Value.Ticks}",
                "Ya pasaste bastante el descanso indicado. Cuando quieras, seguimos con la próxima serie.",
                $"El descanso actual superó en más de {LongRestGraceSeconds} segundos los {exercise.DescansoObjetivoSegundos} segundos indicados.");
        }

        private static LiveCoachProactiveCandidate? EvaluateTimeDrift(
            SesionEntrenamiento session,
            LiveCoachTimeConstraintReference? constraint,
            DateTime nowUtc)
        {
            if (constraint is null)
                return null;
            if (nowUtc < constraint.CapturedAtUtc ||
                nowUtc - constraint.CapturedAtUtc > TimeConstraintValidity)
            {
                return null;
            }
            var elapsedMinutes = Math.Max(0, (nowUtc - constraint.CapturedAtUtc).TotalMinutes);
            var availableMinutes = (int)Math.Floor(constraint.AvailableMinutes - elapsedMinutes);
            if (availableMinutes <= 0)
                return null;
            var estimatedMinutes = LiveTimeConstraintAdaptationEngine.EstimatePendingMinutes(session);
            if (estimatedMinutes < availableMinutes + TimeDriftToleranceMinutes)
                return null;

            return new(
                LiveCoachProactiveTriggerType.SessionTimeDrift,
                null,
                $"{constraint.CapturedAtUtc.Ticks}:{availableMinutes}:{LiveTimeConstraintFingerprint.Create(session)}",
                "Con el ritmo actual, lo pendiente vuelve a superar el tiempo que me dijiste. Puedo recalcular el ajuste.",
                $"Quedan aproximadamente {availableMinutes} minutos del límite informado y lo pendiente se estima en {estimatedMinutes} minutos.",
                "Recalcular ajuste",
                $"Me quedan {availableMinutes} minutos; recalculá el ajuste de la sesión.");
        }

        private static int GetPriority(LiveCoachProactiveTriggerType type)
            => type switch
            {
                LiveCoachProactiveTriggerType.RepeatedUnderperformance => 1,
                LiveCoachProactiveTriggerType.ExcessiveEffort => 2,
                LiveCoachProactiveTriggerType.SessionTimeDrift => 3,
                LiveCoachProactiveTriggerType.LongRest => 4,
                _ => int.MaxValue
            };
    }
}
