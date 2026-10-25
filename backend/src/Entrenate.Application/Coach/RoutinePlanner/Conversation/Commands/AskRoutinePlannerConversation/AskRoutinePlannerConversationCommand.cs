using MediatR;

namespace Entrenate.Application.Coach.RoutinePlanner.Conversation.Commands.AskRoutinePlannerConversation
{
    public sealed record AskRoutinePlannerConversationCommand(
        Guid ProposalId,
        string? ProposalHash,
        string Question)
        : IRequest<RoutinePlannerConversationResponse>;
}
