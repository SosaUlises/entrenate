using Entrenate.Application.Coach.AdaptiveTraining.Explanations;

namespace Entrenate.Application.Coach.AdaptiveTraining.Conversation
{
    public sealed record SessionAdaptationConversationContext(
        string Feasibility,
        bool CanBeConfirmed,
        int? AvailableMinutes,
        string EffortPreference,
        int OriginalDurationMinutes,
        int AdaptedDurationMinutes,
        IReadOnlyList<SessionAdaptationConversationExercise> OriginalExercises,
        IReadOnlyList<SessionAdaptationConversationExercise> AdaptedExercises,
        IReadOnlyList<SessionAdaptationConversationChange> Changes,
        IReadOnlyList<string> Warnings,
        SessionAdaptationExplanation Explanation);

    public sealed record SessionAdaptationConversationExercise(
        string Name,
        int Order,
        string Role,
        int Sets,
        int MinReps,
        int MaxReps,
        int? MinRir,
        int? MaxRir,
        int RestSeconds,
        string? Notes = null);

    public sealed record SessionAdaptationConversationChange(
        string Description,
        string Category,
        string OriginalExerciseName,
        string? AdaptedExerciseName = null,
        int? OriginalSets = null,
        int? AdaptedSets = null,
        int? OriginalMinRir = null,
        int? OriginalMaxRir = null,
        int? AdaptedMinRir = null,
        int? AdaptedMaxRir = null,
        string? Reason = null);
}

