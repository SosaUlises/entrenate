using Entrenate.Application.Coach.Progress.Analysis;
using Entrenate.Application.Coach.Progress.Explanations;
using Entrenate.Application.Coach.Progress.Recommendations;

namespace Entrenate.Application.Coach.Progress.Conversation
{
    public record ProgressConversationContext(
        Guid ExerciseId,
        int ValidExposureCount,
        int DistinctSessionCount,
        int ComparableExposureCount,
        bool PrescriptionChanged,
        bool LoadChanged,
        ProgressConversationPrescription? CurrentPrescription,
        ProgressConversationLatestExposure? LatestExposure,
        ProgressDecision Decision,
        EvidenceLevel EvidenceLevel,
        IReadOnlyCollection<ReasonCode> ReasonCodes,
        ProgressExplanation Explanation);

    public record ProgressConversationPrescription(
        int PlannedSets,
        int MinReps,
        int MaxReps,
        int MinRir,
        int MaxRir);

    public record ProgressConversationLatestExposure(
        DateTime StartedAt,
        bool IsComplete,
        int CompletedPlannedSetCount,
        int MissingPlannedSetCount,
        int ExtraSetCount,
        int BelowRangeSetCount,
        int InRangeSetCount,
        int TopRangeSetCount,
        decimal? RirCoverageRatio,
        LoadClassification? LoadClassification,
        decimal? ReferenceLoad,
        bool HasRepetitionOverload,
        bool HasRirOverload,
        bool IsTopMastery,
        bool IsOverloaded);
}
