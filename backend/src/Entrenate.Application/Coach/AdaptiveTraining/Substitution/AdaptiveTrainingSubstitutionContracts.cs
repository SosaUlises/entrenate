using Entrenate.Application.Coach.AdaptiveTraining.TimeAdaptation;
using Entrenate.Domain.Enums;

namespace Entrenate.Application.Coach.AdaptiveTraining.Substitution
{
    public sealed record PlannedSubstitutionSession(
        Guid RoutineDayId,
        IReadOnlyList<PlannedSubstitutionExercise> Exercises);

    public sealed record PlannedSubstitutionExercise(
        Guid SourceRoutineExerciseId,
        Guid ExerciseId,
        string? ExerciseCode,
        string Name,
        int Order,
        PatronMovimiento? MovementPattern,
        TipoEjercicio? ExerciseType,
        string PrimaryMuscleGroup,
        IReadOnlyCollection<string> SecondaryMuscleGroups,
        IReadOnlyCollection<Guid> RequiredEquipmentIds,
        bool? IsRequiredForAdaptation,
        AdaptiveTrainingPrescription Prescription);

    public sealed record AdaptiveSubstitutionCandidate(
        Guid ExerciseId,
        string? Code,
        string Name,
        PatronMovimiento? MovementPattern,
        TipoEjercicio? ExerciseType,
        string PrimaryMuscleGroup,
        IReadOnlyCollection<string> SecondaryMuscleGroups,
        bool IsActive,
        IReadOnlyCollection<Guid> RequiredEquipmentIds);

    [Flags]
    public enum AdaptiveSubstitutionReason
    {
        None = 0,
        EquipmentUnavailable = 1,
        UserExcludedExercise = 2
    }

    public enum AdaptiveTrainingSubstitutionFeasibility
    {
        Feasible = 1,
        FeasibleWithChanges = 2,
        NotFeasible = 3
    }

    public enum AdaptiveTrainingSubstitutionChangeType
    {
        Unchanged = 1,
        Substituted = 2,
        OptionalRemoved = 3,
        RequiredUnavailable = 4
    }

    public sealed record AdaptiveTrainingSubstitutionExerciseSnapshot(
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
        AdaptiveTrainingPrescription Prescription);

    public sealed record AdaptiveTrainingSubstitutionSessionSnapshot(
        Guid RoutineDayId,
        IReadOnlyList<AdaptiveTrainingSubstitutionExerciseSnapshot> Exercises);

    public sealed record AdaptiveTrainingSubstitutionChange(
        AdaptiveTrainingSubstitutionChangeType Type,
        AdaptiveSubstitutionReason Reason,
        AdaptiveTrainingSubstitutionExerciseSnapshot OriginalExercise,
        AdaptiveTrainingSubstitutionExerciseSnapshot? AdaptedExercise);

    public enum AdaptiveTrainingSubstitutionDiagnosticCode
    {
        ExerciseSubstituted = 1,
        OptionalExerciseRemovedNoSubstitute = 2,
        RequiredExerciseHasNoSubstitute = 3
    }

    public sealed record AdaptiveTrainingSubstitutionDiagnostic(
        AdaptiveTrainingSubstitutionDiagnosticCode Code,
        Guid? SourceRoutineExerciseId = null,
        Guid? ExerciseId = null,
        string? ExerciseCode = null,
        Guid? SubstitutedExerciseId = null,
        string? SubstitutedExerciseCode = null);

    public sealed record AdaptiveTrainingSubstitutionProposal(
        string PolicyVersion,
        Guid RoutineDayId,
        AdaptiveTrainingSubstitutionFeasibility Feasibility,
        AdaptiveTrainingSubstitutionSessionSnapshot OriginalSession,
        AdaptiveTrainingSubstitutionSessionSnapshot AdaptedSession,
        IReadOnlyList<AdaptiveTrainingSubstitutionChange> Changes,
        IReadOnlyList<AdaptiveTrainingSubstitutionDiagnostic> Diagnostics);

    public sealed record AdaptiveTrainingSubstitutionRequest(
        PlannedSubstitutionSession Session,
        IReadOnlyCollection<AdaptiveSubstitutionCandidate> Catalog,
        IReadOnlyCollection<Guid> AvailableEquipmentIds,
        IReadOnlyCollection<Guid>? ExcludedExerciseIds = null);
}

