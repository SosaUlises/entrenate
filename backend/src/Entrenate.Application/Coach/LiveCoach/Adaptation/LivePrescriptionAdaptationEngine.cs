using Entrenate.Application.Coach.LiveCoach.Proactive;
using Entrenate.Domain.Entidades;

namespace Entrenate.Application.Coach.LiveCoach.Adaptation
{
    public enum LivePrescriptionAdaptationStatus
    {
        NoPendingWork,
        InsufficientEvidence,
        NoSafeChange,
        Proposal
    }

    public sealed record LivePrescriptionAdaptationProposal(
        Guid ProposalId,
        string UserId,
        Guid SessionId,
        Guid ExerciseSessionId,
        string ExerciseName,
        int EffectiveFromSetNumber,
        int PreviousRepMin,
        int PreviousRepMax,
        int PreviousRirMin,
        int PreviousRirMax,
        int NewRepMin,
        int NewRepMax,
        int NewRirMin,
        int NewRirMax,
        LivePerformanceSignalType SignalType,
        string PendingWorkFingerprint,
        DateTime CreatedAtUtc,
        DateTime ExpiresAtUtc)
    {
        public string PublicSummary => SignalType == LivePerformanceSignalType.ExcessiveEffort
            ? $"Ajustar el RIR pendiente de {ExerciseName} de {PreviousRirMin}-{PreviousRirMax} a {NewRirMin}-{NewRirMax}"
            : $"Ajustar las repeticiones pendientes de {ExerciseName} de {PreviousRepMin}-{PreviousRepMax} a {NewRepMin}-{NewRepMax}";

        public string SuccessMessage => SignalType == LivePerformanceSignalType.ExcessiveEffort
            ? $"Listo. Las series pendientes de {ExerciseName} ahora tienen un objetivo RIR {NewRirMin}-{NewRirMax}."
            : $"Listo. Las series pendientes de {ExerciseName} ahora tienen un objetivo de {NewRepMin}-{NewRepMax} repeticiones.";
    }

    public sealed record LivePrescriptionAdaptationResult(
        LivePrescriptionAdaptationStatus Status,
        LivePrescriptionAdaptationProposal? Proposal = null);

    public static class LivePrescriptionAdaptationEngine
    {
        public const int MinimumRepetitions = 4;
        public const int RepetitionReduction = 2;
        public const int RirIncrease = 1;

        public static LivePrescriptionAdaptationResult Evaluate(
            SesionEntrenamiento session,
            string userId,
            LivePerformanceSignalType signalType,
            DateTime nowUtc)
        {
            ArgumentNullException.ThrowIfNull(session);
            var evidence = LiveCoachPerformanceTrendDetector.Detect(session);
            if (evidence is null)
                return new(LivePrescriptionAdaptationStatus.InsufficientEvidence);

            var exercise = evidence.Exercise;
            var completed = exercise.Series
                .Where(set => set.EstaRealmenteCompletada())
                .Select(set => set.NumeroSerie)
                .Where(number => number >= 1 && number <= exercise.SeriesObjetivo)
                .Distinct()
                .ToHashSet();
            var firstPending = Enumerable.Range(1, exercise.SeriesObjetivo)
                .FirstOrDefault(number => !completed.Contains(number));
            if (firstPending == 0)
                return new(LivePrescriptionAdaptationStatus.NoPendingWork);

            var newRepMin = exercise.RepeticionesMinimasObjetivo;
            var newRepMax = exercise.RepeticionesMaximasObjetivo;
            var newRirMin = exercise.RirObjetivoMinimo;
            var newRirMax = exercise.RirObjetivoMaximo;

            if (signalType == LivePerformanceSignalType.ExcessiveEffort)
            {
                if (!evidence.ExcessiveEffort)
                    return new(LivePrescriptionAdaptationStatus.InsufficientEvidence);
                newRirMin = Math.Min(5, newRirMin + RirIncrease);
                newRirMax = Math.Min(5, newRirMax + RirIncrease);
            }
            else
            {
                if (!evidence.RepeatedUnderperformance)
                    return new(LivePrescriptionAdaptationStatus.InsufficientEvidence);
                newRepMin = Math.Max(MinimumRepetitions, newRepMin - RepetitionReduction);
                newRepMax = Math.Max(newRepMin, newRepMax - RepetitionReduction);
            }

            if (newRepMin == exercise.RepeticionesMinimasObjetivo &&
                newRepMax == exercise.RepeticionesMaximasObjetivo &&
                newRirMin == exercise.RirObjetivoMinimo &&
                newRirMax == exercise.RirObjetivoMaximo)
            {
                return new(LivePrescriptionAdaptationStatus.NoSafeChange);
            }

            var expiresAtUtc = nowUtc.AddSeconds(45);
            return new(
                LivePrescriptionAdaptationStatus.Proposal,
                new LivePrescriptionAdaptationProposal(
                    Guid.NewGuid(),
                    userId,
                    session.Id,
                    exercise.Id,
                    exercise.Ejercicio.Nombre,
                    firstPending,
                    exercise.RepeticionesMinimasObjetivo,
                    exercise.RepeticionesMaximasObjetivo,
                    exercise.RirObjetivoMinimo,
                    exercise.RirObjetivoMaximo,
                    newRepMin,
                    newRepMax,
                    newRirMin,
                    newRirMax,
                    signalType,
                    LiveTimeConstraintFingerprint.Create(session),
                    nowUtc,
                    expiresAtUtc));
        }
    }
}
