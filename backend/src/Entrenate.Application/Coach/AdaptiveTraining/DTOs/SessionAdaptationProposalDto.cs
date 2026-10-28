namespace Entrenate.Application.Coach.AdaptiveTraining.DTOs
{
    public sealed record SessionAdaptationProposalDto(
        string ProposalId,
        string ProposalHash,
        string PolicyVersion,
        string Feasibility,
        AdaptiveContextDto Context,
        int OriginalDurationSeconds,
        int AdaptedDurationSeconds,
        IReadOnlyList<AdaptiveExerciseDto> OriginalExercises,
        IReadOnlyList<AdaptiveExerciseDto> AdaptedExercises,
        IReadOnlyList<AdaptiveChangeDto> Changes,
        IReadOnlyList<AdaptiveDiagnosticDto> Diagnostics);

    public sealed record AdaptiveContextDto(
        Guid RoutineDayId,
        int? AvailableMinutes,
        IReadOnlyList<Guid> AvailableEquipmentIds,
        IReadOnlyList<Guid> ExcludedExerciseIds,
        string EffortPreference);

    public sealed record AdaptiveExerciseDto(
        Guid ExerciseId,
        string? Code,
        string Name,
        int Order,
        string? MovementPattern,
        string? ExerciseType,
        string Role,
        int Sets,
        int MinReps,
        int MaxReps,
        int MinRir,
        int MaxRir,
        int RestSeconds,
        string? Notes);

    public sealed record AdaptiveChangeDto(
        string ChangeType,
        Guid SourceRoutineExerciseId,
        Guid OriginalExerciseId,
        string? OriginalExerciseCode,
        Guid? AdaptedExerciseId,
        string? AdaptedExerciseCode,
        int? OriginalSets,
        int? AdaptedSets,
        int? OriginalRirMin,
        int? OriginalRirMax,
        int? AdaptedRirMin,
        int? AdaptedRirMax,
        string? Reason);

    public sealed record AdaptiveDiagnosticDto(
        string Code,
        string Severity,
        string Message);
}

