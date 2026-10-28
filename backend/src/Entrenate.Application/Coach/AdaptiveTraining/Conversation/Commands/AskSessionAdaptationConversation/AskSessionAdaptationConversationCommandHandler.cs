using Entrenate.Application.Coach.AdaptiveTraining.Explanations;
using Entrenate.Application.Coach.AdaptiveTraining.Proposal;
using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using MediatR;

namespace Entrenate.Application.Coach.AdaptiveTraining.Conversation.Commands.AskSessionAdaptationConversation
{
    public sealed class AskSessionAdaptationConversationCommandHandler
        : IRequestHandler<AskSessionAdaptationConversationCommand, SessionAdaptationConversationResponse>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly SessionAdaptationProposalGenerator _proposalGenerator;
        private readonly ICoachConversationProvider _conversationProvider;

        public AskSessionAdaptationConversationCommandHandler(
            ICurrentUserService currentUserService,
            SessionAdaptationProposalGenerator proposalGenerator,
            ICoachConversationProvider conversationProvider)
        {
            _currentUserService = currentUserService;
            _proposalGenerator = proposalGenerator;
            _conversationProvider = conversationProvider;
        }

        public async Task<SessionAdaptationConversationResponse> Handle(
            AskSessionAdaptationConversationCommand request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException("No se pudo identificar al usuario autenticado.");
            }

            var proposal = await _proposalGenerator.GenerateForUserAsync(
                userId,
                request.RoutineDayId,
                request.AvailableMinutes,
                request.AvailableEquipmentIds,
                request.ExcludedExerciseIds,
                request.EffortPreference,
                cancellationToken);

            var proposalIdString = proposal.AdaptationProposalId.ToString();

            if (!string.Equals(proposalIdString, request.ProposalId, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(
                    proposal.ProposalHash,
                    request.ProposalHash?.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new ConflictException("La propuesta cambió y debe regenerarse antes de continuar.");
            }

            var explanation = SessionAdaptationExplanationGenerator.Generate(proposal);
            var context = SessionAdaptationConversationContextBuilder.Build(proposal, explanation);
            var prompt = SessionAdaptationConversationPromptBuilder.Build(request.Question, context);

            try
            {
                var response = await _conversationProvider.GenerateResponseAsync(prompt, cancellationToken);
                return new SessionAdaptationConversationResponse(response.Answer);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new ExternalServiceException(
                    "No se pudo obtener una respuesta de Entrenate Coach.",
                    exception);
            }
        }
    }
}

