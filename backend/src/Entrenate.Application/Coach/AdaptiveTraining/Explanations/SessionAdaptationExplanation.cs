namespace Entrenate.Application.Coach.AdaptiveTraining.Explanations
{
    public sealed record SessionAdaptationExplanation(
        string Summary,
        IReadOnlyList<string> Changes,
        string Rationale,
        IReadOnlyList<string> Warnings);
}

