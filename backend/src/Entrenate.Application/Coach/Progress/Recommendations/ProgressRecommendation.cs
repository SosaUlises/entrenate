namespace Entrenate.Application.Coach.Progress.Recommendations
{
    public record ProgressRecommendation(
        Guid ExerciseId,
        ProgressDecision Decision,
        EvidenceLevel EvidenceLevel,
        IReadOnlyCollection<ReasonCode> ReasonCodes);
}
