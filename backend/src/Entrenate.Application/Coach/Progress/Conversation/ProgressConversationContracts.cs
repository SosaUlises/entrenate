namespace Entrenate.Application.Coach.Progress.Conversation
{
    public record ProgressConversationRequest(
        string Question,
        ProgressConversationContext Context);

    public record ProgressConversationResponse(
        string Answer);
}
