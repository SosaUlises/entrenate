namespace Entrenate.Application.Coach.RoutinePlanner.Conversation
{
    public sealed record RoutinePlannerConversationRequest(
        string Question,
        RoutinePlannerConversationContext Context);
}
