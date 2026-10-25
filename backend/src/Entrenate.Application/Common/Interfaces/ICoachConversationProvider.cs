using Entrenate.Application.Common.Models;

namespace Entrenate.Application.Common.Interfaces
{
    public interface ICoachConversationProvider
    {
        Task<CoachConversationResponse> GenerateResponseAsync(
            CoachConversationPrompt prompt,
            CancellationToken cancellationToken = default);
    }
}
