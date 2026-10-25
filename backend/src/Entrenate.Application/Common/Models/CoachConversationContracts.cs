namespace Entrenate.Application.Common.Models
{
    public sealed record CoachConversationPrompt(
        string Instructions,
        string Input);

    public sealed record CoachConversationResponse(
        string Answer);
}
