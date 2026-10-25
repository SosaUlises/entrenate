namespace Entrenate.Application.Coach.RoutinePlanner.Conversation
{
    public sealed record RoutinePlannerConversationRequest(
        string Question,
        RoutinePlannerConversationContext Context);

    public sealed record RoutinePlannerConversationResponse(
        string Answer);
}
