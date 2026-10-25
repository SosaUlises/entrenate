namespace Entrenate.Application.Coach.RoutinePlanner.DTOs
{
    public sealed record RoutinePlannerProposalDto(
        Guid ProposalId,
        string PolicyVersion,
        string ProposalHash,
        string Feasibility,
        string Goal,
        string ExperienceLevel,
        int RequestedDaysPerWeek,
        IReadOnlyList<RoutinePlannerProposalDayDto> Days,
        IReadOnlyList<RoutinePlannerWeeklyVolumeDto> WeeklyVolumes,
        IReadOnlyList<RoutinePlannerSelectionDiagnosticDto>
            SelectionDiagnostics,
        IReadOnlyList<RoutinePlannerPrescriptionDiagnosticDto>
            PrescriptionDiagnostics);

    public sealed record RoutinePlannerProposalDayDto(
        int Order,
        string ScheduledDay,
        string DayOrigin,
        string Focus,
        string Variant,
        int TargetDurationMinutes,
        int EstimatedDurationSeconds,
        IReadOnlyList<RoutinePlannerProposalExerciseDto> Exercises);

    public sealed record RoutinePlannerProposalExerciseDto(
        Guid ExerciseId,
        string Code,
        string Name,
        int Order,
        string MovementPattern,
        string ExerciseType,
        string Role,
        int Sets,
        int MinReps,
        int MaxReps,
        int MinRir,
        int MaxRir,
        int RestSeconds);

    public sealed record RoutinePlannerWeeklyVolumeDto(
        string MuscleGroup,
        decimal EffectiveSets);

    public sealed record RoutinePlannerSelectionDiagnosticDto(
        string Code,
        int DayOrder,
        int SlotPosition);

    public sealed record RoutinePlannerPrescriptionDiagnosticDto(
        string Code,
        int? DayOrder,
        string? ExerciseCode,
        string? MovementPattern,
        string? MuscleGroup);
}
