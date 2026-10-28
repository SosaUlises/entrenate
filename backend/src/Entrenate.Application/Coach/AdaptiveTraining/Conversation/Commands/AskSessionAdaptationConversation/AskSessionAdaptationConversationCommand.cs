using Entrenate.Application.Coach.AdaptiveTraining.Proposal;
using MediatR;

namespace Entrenate.Application.Coach.AdaptiveTraining.Conversation.Commands.AskSessionAdaptationConversation
{
    public sealed record AskSessionAdaptationConversationCommand(
        string ProposalId,
        string ProposalHash,
        Guid RoutineDayId,
        int? AvailableMinutes,
        IReadOnlyCollection<Guid>? AvailableEquipmentIds,
        IReadOnlyCollection<Guid>? ExcludedExerciseIds,
        SessionEffortPreference EffortPreference,
        string Question)
        : IRequest<SessionAdaptationConversationResponse>;
}

