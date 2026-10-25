using Entrenate.Application.Coach.RoutinePlanner.Explanations;
using Entrenate.Application.Coach.RoutinePlanner.ExerciseSelection;
using Entrenate.Application.Coach.RoutinePlanner.WeeklyStructure;
using Entrenate.Domain.Enums;

namespace Entrenate.Application.Coach.RoutinePlanner.Conversation
{
    public sealed record RoutinePlannerConversationContext(
        ObjetivoEntrenamiento Goal,
        NivelExperiencia ExperienceLevel,
        int DaysPerWeek,
        RoutinePlannerConversationFeasibility Feasibility,
        IReadOnlyList<RoutinePlannerConversationSession> Sessions,
        IReadOnlyList<RoutinePlannerConversationMuscleVolume>
            WeeklyMuscleVolumes,
        IReadOnlyList<string> Diagnostics,
        RoutinePlannerExplanation Explanation);

    public sealed record RoutinePlannerConversationFeasibility(
        bool CanBeConfirmed,
        bool HasLimitations,
        string Description);

    public sealed record RoutinePlannerConversationSession(
        int Order,
        DiaSemana ScheduledDay,
        bool IsPreferredDay,
        RoutineSessionFocus Focus,
        RoutineSessionVariant Variant,
        int TargetDurationMinutes,
        int EstimatedDurationMinutes,
        IReadOnlyList<RoutinePlannerConversationExercise> Exercises);

    public sealed record RoutinePlannerConversationExercise(
        string Name,
        PatronMovimiento MovementPattern,
        TipoEjercicio ExerciseType,
        ExerciseSlotRole Role,
        int Sets,
        int MinReps,
        int MaxReps,
        int MinRir,
        int MaxRir,
        int RestSeconds);

    public sealed record RoutinePlannerConversationMuscleVolume(
        string MuscleGroup,
        decimal EffectiveSets);
}
