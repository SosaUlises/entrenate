using Entrenate.Application.Coach.AdaptiveTraining.TimeAdaptation;
using Entrenate.Domain.Enums;

namespace Entrenate.Application.Coach.AdaptiveTraining.Lighter
{
    public sealed record PlannedLighterSession(
        Guid RoutineDayId,
        IReadOnlyList<PlannedLighterExercise> Exercises);

    public sealed record PlannedLighterExercise(
        Guid SourceRoutineExerciseId,
        Guid ExerciseId,
        string? ExerciseCode,
        string Name,
        int Order,
        PatronMovimiento? MovementPattern,
        TipoEjercicio? ExerciseType,
        string PrimaryMuscleGroup,
        IReadOnlyCollection<string>? SecondaryMuscleGroups,
        IReadOnlyCollection<Guid>? RequiredEquipmentIds,
        bool? IsRequiredForAdaptation,
        AdaptiveTrainingLighterPrescription Prescription);

    public sealed record AdaptiveTrainingLighterPrescription(
        int Sets,
        int MinReps,
        int MaxReps,
        int? MinRir,
        int? MaxRir,
        int RestSeconds,
        string? Notes = null)
    {
        public static AdaptiveTrainingLighterPrescription FromStandard(
            AdaptiveTrainingPrescription standard)
        {
            ArgumentNullException.ThrowIfNull(standard);

            return new AdaptiveTrainingLighterPrescription(
                standard.Sets,
                standard.MinReps,
                standard.MaxReps,
                standard.MinRir,
                standard.MaxRir,
                standard.RestSeconds,
                standard.Notes);
        }
    }

    public enum AdaptiveTrainingLighterFeasibility
    {
        Feasible = 1,
        FeasibleWithChanges = 2
    }

    public enum AdaptiveTrainingLighterChangeType
    {
        RirIncreased = 1,
        OptionalAccessorySetsReduced = 2
    }

    public sealed record AdaptiveTrainingLighterChange(
        AdaptiveTrainingLighterChangeType Type,
        Guid SourceRoutineExerciseId,
        Guid ExerciseId,
        string? ExerciseCode,
        int? OriginalSets = null,
        int? AdaptedSets = null,
        int? OriginalMinRir = null,
        int? OriginalMaxRir = null,
        int? AdaptedMinRir = null,
        int? AdaptedMaxRir = null);

    public enum AdaptiveTrainingLighterDiagnosticCode
    {
        RirIncreased = 1,
        OptionalAccessorySetsReduced = 2,
        NeutralLighterTransformation = 3
    }

    public sealed record AdaptiveTrainingLighterDiagnostic(
        AdaptiveTrainingLighterDiagnosticCode Code,
        Guid? SourceRoutineExerciseId = null,
        Guid? ExerciseId = null,
        string? ExerciseCode = null);

    public sealed record AdaptiveTrainingLighterExerciseSnapshot(
        Guid SourceRoutineExerciseId,
        Guid ExerciseId,
        string? ExerciseCode,
        string Name,
        int Order,
        AdaptiveTrainingExercisePriority Priority,
        PatronMovimiento? MovementPattern,
        TipoEjercicio? ExerciseType,
        string PrimaryMuscleGroup,
        IReadOnlyCollection<string> SecondaryMuscleGroups,
        IReadOnlyCollection<Guid> RequiredEquipmentIds,
        AdaptiveTrainingLighterPrescription Prescription);

    public sealed record AdaptiveTrainingLighterSessionSnapshot(
        Guid RoutineDayId,
        int EstimatedDurationSeconds,
        IReadOnlyList<AdaptiveTrainingLighterExerciseSnapshot> Exercises);

    public sealed record AdaptiveTrainingLighterProposal(
        string PolicyVersion,
        Guid RoutineDayId,
        AdaptiveTrainingLighterFeasibility Feasibility,
        AdaptiveTrainingLighterSessionSnapshot OriginalSession,
        AdaptiveTrainingLighterSessionSnapshot AdaptedSession,
        IReadOnlyList<AdaptiveTrainingLighterChange> Changes,
        IReadOnlyList<AdaptiveTrainingLighterDiagnostic> Diagnostics);
}

