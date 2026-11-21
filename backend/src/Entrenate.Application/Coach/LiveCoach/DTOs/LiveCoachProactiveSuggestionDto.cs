namespace Entrenate.Application.Coach.LiveCoach.DTOs
{
    public sealed record LiveCoachProactiveSuggestionDto(
        string Trigger,
        string Text,
        string? ActionLabel = null,
        string? ActionPrompt = null);

    public sealed record LiveCoachProactiveEvaluationDto(
        LiveCoachProactiveSuggestionDto? Suggestion);
}
