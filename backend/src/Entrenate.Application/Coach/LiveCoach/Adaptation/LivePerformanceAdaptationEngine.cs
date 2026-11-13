using Entrenate.Application.TrainingSessions;
using Entrenate.Domain.Entidades;

namespace Entrenate.Application.Coach.LiveCoach.Adaptation
{
    public enum LivePerformanceSignalType
    {
        PerceivedWeakness,
        MissedRepetitions,
        ExcessiveEffort
    }

    public enum LivePerformanceAdaptationResultStatus
    {
        NoPendingWork,
        InsufficientContext,
        WithinPrescription,
        UnderperformanceWithoutSafeCut,
        Proposal
    }

    public sealed record LivePerformanceEvidence(
        int ActualRepetitions,
        int? ActualRir,
        int TargetMinimumRepetitions,
        int TargetMaximumRepetitions,
        int TargetMinimumRir,
        int TargetMaximumRir,
        bool MissedPrescription,
        bool ExceededTargetEffort);

    public sealed record LivePerformanceAdaptationProposal(
        Guid ProposalId,
        string UserId,
        Guid SessionId,
        DateTime CreatedAtUtc,
        DateTime ExpiresAtUtc,
        LivePerformanceSignalType SignalType,
        LivePerformanceEvidence Evidence,
        string PendingWorkFingerprint,
        IReadOnlyCollection<LiveAdaptationChange> Changes,
        string PublicSummary) : ILiveAdaptationProposal
    {
        public string SuccessMessage =>
            "Listo. Reduje el trabajo posterior de esta sesión.";
    }

    public sealed record LivePerformanceAdaptationResult(
        LivePerformanceAdaptationResultStatus Status,
        LivePerformanceSignalType SignalType,
        Guid? CurrentExerciseSessionId,
        string? CurrentExerciseName,
        LivePerformanceEvidence? Evidence,
        IReadOnlyCollection<LiveAdaptationChange> Changes);

    public static class LivePerformanceAdaptationEngine
    {
        public static LivePerformanceAdaptationResult Evaluate(
            SesionEntrenamiento session,
            LivePerformanceSignalType signalType)
        {
            ArgumentNullException.ThrowIfNull(session);

            var current = CurrentPendingTrainingSetResolver.Resolve(session);
            if (current is null)
            {
                return new(
                    LivePerformanceAdaptationResultStatus.NoPendingWork,
                    signalType,
                    null,
                    null,
                    null,
                    []);
            }

            var lastCompletedSet = current.ExerciseSession.Series
                .Where(set => set.EstaRealmenteCompletada())
                .OrderByDescending(set => set.FechaHoraRegistro)
                .ThenByDescending(set => set.NumeroSerie)
                .FirstOrDefault();
            if (lastCompletedSet is null)
            {
                return new(
                    LivePerformanceAdaptationResultStatus.InsufficientContext,
                    signalType,
                    current.ExerciseSession.Id,
                    current.ExerciseSession.Ejercicio.Nombre,
                    null,
                    []);
            }

            var evidence = new LivePerformanceEvidence(
                lastCompletedSet.Repeticiones,
                lastCompletedSet.Rir,
                current.ExerciseSession.RepeticionesMinimasObjetivo,
                current.ExerciseSession.RepeticionesMaximasObjetivo,
                current.ExerciseSession.RirObjetivoMinimo,
                current.ExerciseSession.RirObjetivoMaximo,
                lastCompletedSet.Repeticiones < current.ExerciseSession.RepeticionesMinimasObjetivo,
                lastCompletedSet.Rir.HasValue &&
                    lastCompletedSet.Rir.Value < current.ExerciseSession.RirObjetivoMinimo);

            if (!evidence.MissedPrescription && !evidence.ExceededTargetEffort)
            {
                return new(
                    LivePerformanceAdaptationResultStatus.WithinPrescription,
                    signalType,
                    current.ExerciseSession.Id,
                    current.ExerciseSession.Ejercicio.Nombre,
                    evidence,
                    []);
            }

            var candidate = session.Ejercicios
                .Where(exercise =>
                    !exercise.Omitido &&
                    exercise.Id != current.ExerciseSession.Id &&
                    GetPendingSetCount(exercise) > 0 &&
                    GetCompletedSetCount(exercise) == 0)
                .OrderByDescending(exercise => exercise.Orden)
                .FirstOrDefault();
            if (candidate is null)
            {
                return new(
                    LivePerformanceAdaptationResultStatus.UnderperformanceWithoutSafeCut,
                    signalType,
                    current.ExerciseSession.Id,
                    current.ExerciseSession.Ejercicio.Nombre,
                    evidence,
                    []);
            }

            return new(
                LivePerformanceAdaptationResultStatus.Proposal,
                signalType,
                current.ExerciseSession.Id,
                current.ExerciseSession.Ejercicio.Nombre,
                evidence,
                [new LiveAdaptationChange(
                    LiveAdaptationChangeType.OmitExercise,
                    candidate.Id,
                    candidate.Ejercicio.Nombre)]);
        }

        private static int GetPendingSetCount(EjercicioSesion exercise)
            => Math.Max(0, exercise.SeriesObjetivo - GetCompletedSetCount(exercise));

        private static int GetCompletedSetCount(EjercicioSesion exercise)
            => exercise.Series
                .Where(set => set.EstaRealmenteCompletada())
                .Select(set => set.NumeroSerie)
                .Distinct()
                .Count(number => number >= 1 && number <= exercise.SeriesObjetivo);
    }
}
