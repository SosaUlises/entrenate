namespace Entrenate.Application.Coach.Progress.Conversation
{
    public record ProgressConversationRequest(
        string Question,
        ProgressConversationContext Context);

    public record ProgressConversationPrompt(
        string Instructions,
        string Input);

    public record ProgressConversationResponse(
        string Answer);
}
