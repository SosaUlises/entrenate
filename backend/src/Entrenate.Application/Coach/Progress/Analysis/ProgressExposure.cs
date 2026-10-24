namespace Entrenate.Application.Coach.Progress.Analysis
{
    public record ProgressPrescriptionSnapshot(
        int PlannedSets,
        int MinReps,
        int MaxReps,
        int MinRir,
        int MaxRir,
        int RestSeconds);

    public record ProgressCompletedSet(
        int SetNumber,
        decimal Weight,
        int Repetitions,
        int? Rir);

    public record ProgressExposure(
        Guid ExerciseSessionId,
        Guid SessionId,
        Guid ExerciseId,
        DateTime StartedAt,
        ProgressPrescriptionSnapshot Prescription,
        IReadOnlyCollection<ProgressCompletedSet> CompletedSets);
}
