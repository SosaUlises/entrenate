using Entrenate.Application.TrainingSessions;
using Entrenate.Domain.Entidades;

namespace Entrenate.Application.Coach.LiveCoach.Adaptation
{
    public enum LiveAdaptationReason
    {
        TimeConstraint,
        Fatigue,
        Performance
    }

    public sealed record LiveAdaptationSignals(
        int? RemainingMinutes = null,
        LiveFatigueSeverity? FatigueSeverity = null,
        LivePerformanceSignalType? PerformanceSignalType = null);

    public enum LiveCombinedAdaptationStatus
    {
        NoPendingWork,
        NoApplicableChange,
        Proposal
    }

    public sealed record LiveCombinedAdaptationProposal(
        Guid ProposalId,
        string UserId,
        Guid SessionId,
        DateTime CreatedAtUtc,
        DateTime ExpiresAtUtc,
        LiveAdaptationSignals Signals,
        IReadOnlyCollection<LiveAdaptationReason> Reasons,
        LivePerformanceEvidence? PerformanceEvidence,
        int EstimatedMinutesBefore,
        int EstimatedMinutesAfter,
        string PendingWorkFingerprint,
        IReadOnlyCollection<LiveAdaptationChange> Changes,
        string PublicSummary) : ILiveAdaptationProposal
    {
        public string SuccessMessage =>
            "Listo. Apliqué el ajuste combinado a esta sesión.";
    }

    public sealed record LiveCombinedAdaptationResult(
        LiveCombinedAdaptationStatus Status,
        LiveAdaptationSignals Signals,
        IReadOnlyCollection<LiveAdaptationReason> AppliedReasons,
        LivePerformanceEvidence? PerformanceEvidence,
        Guid? CurrentExerciseSessionId,
        string? CurrentExerciseName,
        int EstimatedMinutesBefore,
        int EstimatedMinutesAfter,
        IReadOnlyCollection<LiveAdaptationChange> Changes);

    public static class LiveAdaptationOrchestrator
    {
        public static LiveCombinedAdaptationResult Evaluate(
            SesionEntrenamiento session,
            LiveAdaptationSignals signals)
        {
            ArgumentNullException.ThrowIfNull(session);
            ArgumentNullException.ThrowIfNull(signals);

            var current = CurrentPendingTrainingSetResolver.Resolve(session);
            if (current is null)
            {
                return new(
                    LiveCombinedAdaptationStatus.NoPendingWork,
                    signals,
                    [],
                    null,
                    null,
                    null,
                    0,
                    0,
                    []);
            }

            var changes = new Dictionary<Guid, LiveAdaptationChange>();
            var reasons = new List<LiveAdaptationReason>();
            LivePerformanceEvidence? performanceEvidence = null;
            var beforeMinutes = LiveTimeConstraintAdaptationEngine.EstimatePendingMinutes(session);

            if (signals.RemainingMinutes.HasValue)
            {
                var time = LiveTimeConstraintAdaptationEngine.Evaluate(
                    session,
                    signals.RemainingMinutes.Value);
                AddChanges(changes, time.Changes, current.ExerciseSession.Id);
                if (time.Status == LiveTimeConstraintResultStatus.Proposal)
                    reasons.Add(LiveAdaptationReason.TimeConstraint);
            }

            if (signals.FatigueSeverity.HasValue)
            {
                var fatigue = LiveFatigueAdaptationEngine.Evaluate(
                    session,
                    signals.FatigueSeverity.Value);
                AddChanges(changes, fatigue.Changes, current.ExerciseSession.Id);
                if (fatigue.Status == LiveFatigueAdaptationResultStatus.Proposal)
                    reasons.Add(LiveAdaptationReason.Fatigue);
            }

            if (signals.PerformanceSignalType.HasValue)
            {
                var performance = LivePerformanceAdaptationEngine.Evaluate(
                    session,
                    signals.PerformanceSignalType.Value);
                performanceEvidence = performance.Evidence;
                AddChanges(changes, performance.Changes, current.ExerciseSession.Id);
                if (performance.Status == LivePerformanceAdaptationResultStatus.Proposal)
                    reasons.Add(LiveAdaptationReason.Performance);
            }

            var orderedChanges = changes.Values
                .OrderBy(change => session.Ejercicios
                    .Single(exercise => exercise.Id == change.ExerciseSessionId).Orden)
                .ToArray();
            var afterMinutes = LiveTimeConstraintAdaptationEngine.EstimatePendingMinutes(
                session,
                orderedChanges.Select(change => change.ExerciseSessionId));

            return new(
                orderedChanges.Length == 0
                    ? LiveCombinedAdaptationStatus.NoApplicableChange
                    : LiveCombinedAdaptationStatus.Proposal,
                signals,
                reasons.Distinct().ToArray(),
                performanceEvidence,
                current.ExerciseSession.Id,
                current.ExerciseSession.Ejercicio.Nombre,
                beforeMinutes,
                afterMinutes,
                orderedChanges);
        }

        private static void AddChanges(
            IDictionary<Guid, LiveAdaptationChange> target,
            IEnumerable<LiveAdaptationChange> changes,
            Guid currentExerciseSessionId)
        {
            foreach (var change in changes)
            {
                if (change.Type != LiveAdaptationChangeType.OmitExercise ||
                    change.ExerciseSessionId == currentExerciseSessionId)
                {
                    continue;
                }

                target.TryAdd(change.ExerciseSessionId, change);
            }
        }
    }
}
