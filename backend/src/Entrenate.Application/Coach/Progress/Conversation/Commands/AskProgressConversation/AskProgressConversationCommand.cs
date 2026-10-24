using MediatR;

namespace Entrenate.Application.Coach.Progress.Conversation.Commands.AskProgressConversation
{
    public record AskProgressConversationCommand(
        Guid ExerciseId,
        string Question) : IRequest<ProgressConversationResponse>;
}
