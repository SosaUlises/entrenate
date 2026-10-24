namespace Entrenate.Application.Coach.Progress.Recommendations
{
    public enum ReasonCode
    {
        InsufficientHistory = 1,
        PartialExposure = 2,
        RepeatedPartialExposure = 3,
        WithinRepRange = 4,
        TopRangeReached = 5,
        TopRangeConsistent = 6,
        BelowMinimumReps = 7,
        RirOnTarget = 8,
        RirTooLow = 9,
        RirMissing = 10,
        StableLoad = 11,
        MixedLoad = 12,
        ZeroExternalLoad = 13,
        RecentPerformanceDrop = 14,
        PrescriptionChanged = 15,
        LoadChanged = 16
    }
}
