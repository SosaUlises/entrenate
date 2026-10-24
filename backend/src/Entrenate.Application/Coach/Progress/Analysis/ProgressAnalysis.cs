namespace Entrenate.Application.Coach.Progress.Analysis
{
    public record ProgressAnalysis(
        Guid ExerciseId,
        int ValidExposureCount,
        int DistinctSessionCount,
        IReadOnlyCollection<ProgressExposureAnalysis> Exposures,
        ProgressExposureAnalysis? LatestExposure,
        IReadOnlyCollection<ProgressExposureAnalysis> ComparableExposures,
        int ComparableExposureCount,
        bool PrescriptionChanged,
        bool LoadChanged);
}
