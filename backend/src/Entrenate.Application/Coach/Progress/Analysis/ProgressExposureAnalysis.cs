namespace Entrenate.Application.Coach.Progress.Analysis
{
    public record ProgressEvaluatedSetAnalysis(
        int SetNumber,
        decimal Weight,
        int Repetitions,
        int? Rir,
        RepClassification RepClassification,
        RirClassification RirClassification);

    public record ProgressExposureAnalysis(
        Guid ExerciseSessionId,
        Guid SessionId,
        Guid ExerciseId,
        DateTime StartedAt,
        ProgressPrescriptionSnapshot Prescription,
        ExposureCompleteness Completeness,
        int CompletedPlannedSets,
        IReadOnlyCollection<ProgressEvaluatedSetAnalysis> EvaluatedSets,
        IReadOnlyCollection<ProgressCompletedSet> ExtraSets,
        IReadOnlyCollection<int> MissingPlannedSetNumbers,
        decimal? RirCoverage,
        LoadClassification? LoadClassification,
        decimal? ReferenceLoad,
        bool HasRepetitionOverload,
        bool HasRirOverload,
        bool IsTopMastery,
        bool IsOverloaded);
}
