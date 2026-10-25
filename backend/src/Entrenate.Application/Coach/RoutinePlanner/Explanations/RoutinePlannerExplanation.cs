namespace Entrenate.Application.Coach.RoutinePlanner.Explanations
{
    public sealed record RoutinePlannerExplanation(
        string Summary,
        string Structure,
        string Rationale,
        IReadOnlyList<string> Warnings);
}
