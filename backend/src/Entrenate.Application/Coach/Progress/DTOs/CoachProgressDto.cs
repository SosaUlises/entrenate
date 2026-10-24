namespace Entrenate.Application.Coach.Progress.DTOs
{
    public record CoachProgressDto(
        Guid ExerciseId,
        CoachProgressAnalysisDto Analysis,
        CoachProgressRecommendationDto Recommendation,
        CoachProgressExplanationDto Explanation);

    public record CoachProgressAnalysisDto(
        int ValidExposureCount,
        int DistinctSessionCount,
        int ComparableExposureCount,
        bool PrescriptionChanged,
        bool LoadChanged,
        CoachProgressLatestExposureDto? LatestExposure);

    public record CoachProgressLatestExposureDto(
        Guid ExerciseSessionId,
        Guid SessionId,
        DateTime StartedAt,
        bool IsComplete,
        int CompletedPlannedSetCount,
        int MissingPlannedSetCount,
        int ExtraSetCount,
        string? LoadClassification,
        decimal? ReferenceLoad,
        decimal? RirCoverage,
        bool IsTopMastery,
        bool IsOverloaded,
        CoachProgressPrescriptionDto Prescription);

    public record CoachProgressPrescriptionDto(
        int PlannedSets,
        int MinReps,
        int MaxReps,
        int MinRir,
        int MaxRir,
        int RestSeconds);

    public record CoachProgressRecommendationDto(
        string Decision,
        string EvidenceLevel,
        IReadOnlyCollection<string> ReasonCodes);

    public record CoachProgressExplanationDto(
        string Detection,
        string Recommendation,
        string Rationale);
}
