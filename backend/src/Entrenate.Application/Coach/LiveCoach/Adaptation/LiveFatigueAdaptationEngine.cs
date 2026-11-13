using Entrenate.Application.TrainingSessions;
using Entrenate.Domain.Entidades;

namespace Entrenate.Application.Coach.LiveCoach.Adaptation
{
    public enum LiveFatigueSeverity
    {
        Moderate,
        High
    }

    public enum LiveFatigueAdaptationResultStatus
    {
        NoPendingWork,
        NothingToReduce,
        Proposal
    }

    public sealed record LiveFatigueAdaptationProposal(
        Guid ProposalId,
        string UserId,
        Guid SessionId,
        DateTime CreatedAtUtc,
        DateTime ExpiresAtUtc,
        LiveFatigueSeverity FatigueSeverity,
        string PendingWorkFingerprint,
        IReadOnlyCollection<LiveAdaptationChange> Changes,
        string PublicSummary) : ILiveAdaptationProposal
    {
        public string SuccessMessage =>
            "Listo. Ajusté el trabajo pendiente para esta sesión.";
    }

    public sealed record LiveFatigueAdaptationResult(
        LiveFatigueAdaptationResultStatus Status,
        LiveFatigueSeverity Severity,
        Guid? CurrentExerciseSessionId,
        string? CurrentExerciseName,
        IReadOnlyCollection<LiveAdaptationChange> Changes);

    public static class LiveFatigueAdaptationEngine
    {
        public static LiveFatigueAdaptationResult Evaluate(
            SesionEntrenamiento session,
            LiveFatigueSeverity severity)
        {
            ArgumentNullException.ThrowIfNull(session);

            var current = CurrentPendingTrainingSetResolver.Resolve(session);
            if (current is null)
            {
                return new(
                    LiveFatigueAdaptationResultStatus.NoPendingWork,
                    severity,
                    null,
                    null,
                    []);
            }

            var pendingAfterCurrent = session.Ejercicios
                .Where(exercise =>
                    !exercise.Omitido &&
                    exercise.Id != current.ExerciseSession.Id &&
                    GetPendingSetCount(exercise) > 0)
                .OrderBy(exercise => exercise.Orden)
                .Select(exercise => new Candidate(
                    exercise.Id,
                    exercise.Ejercicio.Nombre,
                    exercise.Orden,
                    GetCompletedSetCount(exercise) > 0))
                .ToList();

            var selected = severity == LiveFatigueSeverity.Moderate
                ? SelectModerate(pendingAfterCurrent)
                : SelectHigh(pendingAfterCurrent);
            if (selected.Count == 0)
            {
                return new(
                    LiveFatigueAdaptationResultStatus.NothingToReduce,
                    severity,
                    current.ExerciseSession.Id,
                    current.ExerciseSession.Ejercicio.Nombre,
                    []);
            }

            var changes = selected
                .OrderBy(candidate => candidate.Order)
                .Select(candidate => new LiveAdaptationChange(
                    LiveAdaptationChangeType.OmitExercise,
                    candidate.ExerciseSessionId,
                    candidate.Name))
                .ToArray();

            return new(
                LiveFatigueAdaptationResultStatus.Proposal,
                severity,
                current.ExerciseSession.Id,
                current.ExerciseSession.Ejercicio.Nombre,
                changes);
        }

        // Moderate removes one final unstarted exercise per three candidates,
        // with a minimum of one whenever at least one safe candidate exists.
        private static IReadOnlyCollection<Candidate> SelectModerate(
            IReadOnlyCollection<Candidate> candidates)
        {
            var unstarted = candidates
                .Where(candidate => !candidate.Started)
                .OrderByDescending(candidate => candidate.Order)
                .ToArray();
            if (unstarted.Length == 0)
                return [];

            var removalCount = Math.Max(1, unstarted.Length / 3);
            return unstarted.Take(removalCount).ToArray();
        }

        // High removes every unstarted exercise. When only started work remains,
        // it preserves the earliest started exercise after current and removes later ones.
        private static IReadOnlyCollection<Candidate> SelectHigh(
            IReadOnlyCollection<Candidate> candidates)
        {
            var unstarted = candidates
                .Where(candidate => !candidate.Started)
                .ToList();
            var started = candidates
                .Where(candidate => candidate.Started)
                .OrderBy(candidate => candidate.Order)
                .Skip(1);

            return unstarted
                .Concat(started)
                .OrderByDescending(candidate => candidate.Order)
                .ToArray();
        }

        private static int GetPendingSetCount(EjercicioSesion exercise)
            => Math.Max(0, exercise.SeriesObjetivo - GetCompletedSetCount(exercise));

        private static int GetCompletedSetCount(EjercicioSesion exercise)
            => exercise.Series
                .Where(set => set.EstaRealmenteCompletada())
                .Select(set => set.NumeroSerie)
                .Distinct()
                .Count(number => number >= 1 && number <= exercise.SeriesObjetivo);

        private sealed record Candidate(
            Guid ExerciseSessionId,
            string Name,
            int Order,
            bool Started);
    }
}
