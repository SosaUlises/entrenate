using Entrenate.Domain.Enums;

namespace Entrenate.Application.Coach.AdaptiveTraining.TimeAdaptation
{
    public sealed record PlannedAdaptiveTrainingSession(
        Guid RoutineDayId,
        IReadOnlyList<PlannedAdaptiveTrainingExercise> Exercises);

    public sealed record PlannedAdaptiveTrainingExercise(
        Guid SourceRoutineExerciseId,
        Guid ExerciseId,
        string? ExerciseCode,
        string Name,
        int Order,
        PatronMovimiento? MovementPattern,
        TipoEjercicio? ExerciseType,
        string PrimaryMuscleGroup,
        bool? IsRequiredForAdaptation,
        AdaptiveTrainingPrescription Prescription);

    public sealed record AdaptiveTrainingPrescription(
        int Sets,
        int MinReps,
        int MaxReps,
        int MinRir,
        int MaxRir,
        int RestSeconds,
        string? Notes);

    public enum AdaptiveTrainingExercisePriority
    {
        RequiredCompound = 1,
        RequiredIsolation = 2,
        OptionalAccessory = 3,
        UnclassifiedRequired = 4
    }

    public sealed record AdaptiveTrainingExerciseSnapshot(
        Guid SourceRoutineExerciseId,
        Guid ExerciseId,
        string? ExerciseCode,
        string Name,
        int Order,
        AdaptiveTrainingExercisePriority Priority,
        AdaptiveTrainingPrescription Prescription);

    public sealed record AdaptiveTrainingSessionSnapshot(
        int EstimatedDurationSeconds,
        IReadOnlyList<AdaptiveTrainingExerciseSnapshot> Exercises);

    public enum AdaptiveTrainingTimeFeasibility
    {
        Feasible = 1,
        FeasibleWithChanges = 2,
        NotFeasible = 3
    }

    public enum AdaptiveTrainingTimeChangeType
    {
        OptionalAccessoryRemoved = 1,
        ExerciseSetsReduced = 2
    }

    public sealed record AdaptiveTrainingTimeChange(
        AdaptiveTrainingTimeChangeType Type,
        Guid SourceRoutineExerciseId,
        Guid ExerciseId,
        string? ExerciseCode,
        int OriginalSets,
        int AdaptedSets);

    public enum AdaptiveTrainingTimeDiagnosticCode
    {
        OptionalAccessoryRemovedForTime = 1,
        ExerciseSetsReducedForTime = 2,
        MinimumSessionExceedsAvailableTime = 3
    }

    public sealed record AdaptiveTrainingTimeDiagnostic(
        AdaptiveTrainingTimeDiagnosticCode Code,
        Guid? SourceRoutineExerciseId = null,
        Guid? ExerciseId = null,
        string? ExerciseCode = null);

    public sealed record AdaptiveTrainingTimeProposal(
        string PolicyVersion,
        Guid RoutineDayId,
        int AvailableMinutes,
        AdaptiveTrainingTimeFeasibility Feasibility,
        AdaptiveTrainingSessionSnapshot OriginalSession,
        AdaptiveTrainingSessionSnapshot AdaptedSession,
        IReadOnlyList<AdaptiveTrainingTimeChange> Changes,
        IReadOnlyList<AdaptiveTrainingTimeDiagnostic> Diagnostics);
}
