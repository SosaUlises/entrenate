using Entrenate.Application.Coach.RoutinePlanner.Explanations;
using Entrenate.Application.Coach.RoutinePlanner.Proposal;
using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using MediatR;

namespace Entrenate.Application.Coach.RoutinePlanner.Conversation.Commands.AskRoutinePlannerConversation
{
    public sealed class AskRoutinePlannerConversationCommandHandler
        : IRequestHandler<
            AskRoutinePlannerConversationCommand,
            RoutinePlannerConversationResponse>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly RoutinePlannerProposalGenerator _proposalGenerator;
        private readonly ICoachConversationProvider _conversationProvider;

        public AskRoutinePlannerConversationCommandHandler(
            ICurrentUserService currentUserService,
            RoutinePlannerProposalGenerator proposalGenerator,
            ICoachConversationProvider conversationProvider)
        {
            _currentUserService = currentUserService;
            _proposalGenerator = proposalGenerator;
            _conversationProvider = conversationProvider;
        }

        public async Task<RoutinePlannerConversationResponse> Handle(
            AskRoutinePlannerConversationCommand request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException(
                    "No se pudo identificar al usuario autenticado.");
            }

            var proposal = await _proposalGenerator.GenerateForUserAsync(
                userId,
                cancellationToken);

            if (proposal.ProposalId != request.ProposalId ||
                !string.Equals(
                    proposal.CanonicalHash,
                    request.ProposalHash?.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new ConflictException(
                    "La propuesta cambió y debe regenerarse antes de " +
                    "continuar.");
            }

            var explanation = RoutinePlannerExplanationGenerator.Generate(
                proposal);
            var context = RoutinePlannerConversationContextBuilder.Build(
                proposal,
                explanation);
            var prompt = RoutinePlannerConversationPromptBuilder.Build(
                new RoutinePlannerConversationRequest(
                    request.Question,
                    context));

            try
            {
                var response = await _conversationProvider
                    .GenerateResponseAsync(
                        prompt,
                        cancellationToken);

                return new RoutinePlannerConversationResponse(
                    response.Answer);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
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
