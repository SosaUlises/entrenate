using Entrenate.Application.Coach.Progress.Conversation;

namespace Entrenate.Application.Common.Interfaces
{
    public interface IProgressConversationProvider
    {
        Task<ProgressConversationResponse> GenerateResponseAsync(
            ProgressConversationPrompt prompt,
            CancellationToken cancellationToken = default);
    }
}
