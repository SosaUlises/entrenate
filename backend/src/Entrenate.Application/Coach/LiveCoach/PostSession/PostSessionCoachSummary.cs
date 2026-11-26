namespace Entrenate.Application.Coach.LiveCoach.PostSession
{
    public sealed record PostSessionBestSetDto(
        string Criterion,
        string ExerciseName,
        int SetNumber,
        decimal WeightKg,
        int Repetitions,
        int? Rir,
        decimal Volume);

    public sealed record PostSessionAdaptationDto(
        string Type,
        string Label,
        string? Detail = null);

    public sealed record PostSessionExerciseSummaryDto(
        Guid ExerciseSessionId,
        string ExerciseName,
        bool Completed,
        bool Omitted,
        int CompletedSets,
        int MissedRepetitionSets,
        int ExcessiveEffortSets,
        int AssessedSets,
        int AffectedSets,
        int MaxRepetitionShortfall,
        int MaxRirShortfall,
        int RepetitionTargetMetSets);

    public sealed record PostSessionCoachSummaryDto(
        Guid SessionId,
        int TotalExercises,
        int CompletedExercises,
        int OmittedExercises,
        int CompletedSets,
        int UnderperformanceEvents,
        int ExcessiveEffortEvents,
        IReadOnlyCollection<PostSessionAdaptationDto> AdaptationsApplied,
        IReadOnlyCollection<PostSessionBestSetDto> BestSets,
        IReadOnlyCollection<PostSessionExerciseSummaryDto> Exercises,
        IReadOnlyCollection<string> NotableObservations);

    public sealed record PostSessionCoachAnswerDto(string Answer);
}
