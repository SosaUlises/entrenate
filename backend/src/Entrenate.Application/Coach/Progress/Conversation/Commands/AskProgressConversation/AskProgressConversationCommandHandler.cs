using Entrenate.Application.Coach.Progress.Analysis;
using Entrenate.Application.Coach.Progress.Explanations;
using Entrenate.Application.Coach.Progress.Recommendations;
using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using MediatR;

namespace Entrenate.Application.Coach.Progress.Conversation.Commands.AskProgressConversation
{
    public class AskProgressConversationCommandHandler
        : IRequestHandler<
            AskProgressConversationCommand,
            ProgressConversationResponse>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IExerciseRepository _exerciseRepository;
        private readonly ITrainingSessionRepository _sessionRepository;
        private readonly IProgressConversationProvider _conversationProvider;

        public AskProgressConversationCommandHandler(
            ICurrentUserService currentUserService,
            IExerciseRepository exerciseRepository,
            ITrainingSessionRepository sessionRepository,
            IProgressConversationProvider conversationProvider)
        {
            _currentUserService = currentUserService;
            _exerciseRepository = exerciseRepository;
            _sessionRepository = sessionRepository;
            _conversationProvider = conversationProvider;
        }

        public async Task<ProgressConversationResponse> Handle(
            AskProgressConversationCommand request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException(
                    "No se pudo identificar al usuario autenticado.");
            }

            var exercise = await _exerciseRepository.GetByIdAsync(
                request.ExerciseId,
                cancellationToken);

            if (exercise is null)
            {
                throw new NotFoundException("El ejercicio no existe.");
            }

            var exposures = await _sessionRepository
                .GetRecentCompletedExerciseExposuresByUserIdAsync(
                    exercise.Id,
                    userId,
                    cancellationToken);

            var analysis = ProgressAnalyzer.Analyze(
                exercise.Id,
                exposures);
            var recommendation = ProgressRecommendationEngine.Recommend(
                analysis);
            var explanation = ProgressExplanationGenerator.Generate(
                recommendation,
                analysis);
            var context = ProgressConversationContextBuilder.Build(
                analysis,
                recommendation,
                explanation);
            var prompt = ProgressConversationPromptBuilder.Build(
                new ProgressConversationRequest(
                    request.Question,
                    context));

            try
            {
                return await _conversationProvider.GenerateResponseAsync(
                    prompt,
                    cancellationToken);
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
