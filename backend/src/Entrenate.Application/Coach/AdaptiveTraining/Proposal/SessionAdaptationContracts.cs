using Entrenate.Application.Coach.AdaptiveTraining.Substitution;
using Entrenate.Application.Coach.AdaptiveTraining.TimeAdaptation;
using Entrenate.Domain.Enums;

namespace Entrenate.Application.Coach.AdaptiveTraining.Proposal
{
    public enum SessionEffortPreference
    {
        Normal = 1,
        Lighter = 2
    }

    public sealed record SessionAdaptationRequest(
        Guid RoutineDayId,
        int? AvailableMinutes = null,
        IReadOnlyCollection<Guid>? AvailableEquipmentIds = null,
        IReadOnlyCollection<Guid>? ExcludedExerciseIds = null,
        SessionEffortPreference EffortPreference = SessionEffortPreference.Normal);

    public sealed record PlannedAdaptiveSession(
        Guid RoutineDayId,
        IReadOnlyList<PlannedAdaptiveExercise> Exercises,
        Guid? RoutineId = null,
        DateTime? RoutineUpdatedAtUtc = null);

    public sealed record PlannedAdaptiveExercise(
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
        AdaptiveTrainingPrescription Prescription);

    public enum SessionAdaptationFeasibility
    {
        Feasible = 1,
        FeasibleWithChanges = 2,
        NotFeasible = 3
    }

    public enum SessionAdaptationChangeType
    {
        ExerciseSubstituted = 1,
        OptionalAccessoryRemovedNoSubstitute = 2,
        RequiredExerciseUnavailable = 3,
        ExerciseSetsReducedForTime = 4,
        OptionalAccessoryRemovedForTime = 5,
        RirIncreased = 6,
        OptionalAccessorySetsReducedForLighter = 7
    }

    public sealed record SessionAdaptationChange(
        SessionAdaptationChangeType ChangeType,
        Guid SourceRoutineExerciseId,
        Guid ExerciseId,
        string? ExerciseCode,
        Guid? SubstitutedExerciseId = null,
        string? SubstitutedExerciseCode = null,
        int? OriginalSets = null,
        int? AdaptedSets = null,
        int? OriginalMinRir = null,
        int? OriginalMaxRir = null,
        int? AdaptedMinRir = null,
        int? AdaptedMaxRir = null,
        AdaptiveSubstitutionReason? SubstitutionReason = null);

    public enum SessionAdaptationDiagnosticCode
    {
        ExerciseSubstituted = 1,
        OptionalExerciseRemovedNoSubstitute = 2,
        RequiredExerciseHasNoSubstitute = 3,
        OptionalAccessoryRemovedForTime = 4,
        ExerciseSetsReducedForTime = 5,
        MinimumSessionExceedsAvailableTime = 6,
        RirIncreased = 7,
        OptionalAccessorySetsReducedForLighter = 8,
        NeutralLighterTransformation = 9
    }

    public sealed record SessionAdaptationDiagnostic(
        SessionAdaptationDiagnosticCode Code,
        Guid? SourceRoutineExerciseId = null,
        Guid? ExerciseId = null,
        string? ExerciseCode = null,
        Guid? SubstitutedExerciseId = null,
        string? SubstitutedExerciseCode = null);

    public sealed record SessionPlanSnapshot(
        int EstimatedDurationSeconds,
        IReadOnlyList<SessionExerciseSnapshot> Exercises);

    public sealed record SessionExerciseSnapshot(
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

    public sealed record SessionAdaptationSource(
        Guid? RoutineId,
        Guid RoutineDayId,
        DateTime? RoutineUpdatedAtUtc);

    public sealed record SessionAdaptationContext(
        int? AvailableMinutes,
        IReadOnlyCollection<Guid> AvailableEquipmentIds,
        IReadOnlyCollection<Guid> ExcludedExerciseIds,
        SessionEffortPreference EffortPreference);

    public sealed record SessionAdaptationProposal(
        Guid AdaptationProposalId,
        string PolicyVersion,
        string ProposalHash,
        SessionAdaptationFeasibility Feasibility,
        SessionAdaptationSource Source,
        SessionAdaptationContext Context,
        SessionPlanSnapshot OriginalSession,
        SessionPlanSnapshot AdaptedSession,
        IReadOnlyList<SessionAdaptationChange> Changes,
        IReadOnlyList<SessionAdaptationDiagnostic> Diagnostics);
}

